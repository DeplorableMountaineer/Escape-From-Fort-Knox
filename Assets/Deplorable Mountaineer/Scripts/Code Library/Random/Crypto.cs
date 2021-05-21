#region

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using Deplorable_Mountaineer.EditorUtils.Attributes;
using Deplorable_Mountaineer.Singleton;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library {
    /// <summary>
    ///     Modification of code
    ///     from a StackOverflow answer
    ///     Crypto for strings, serializable objects, files,
    ///     and player prefs, with default password
    /// </summary>
    [PublicAPI]
    public class Crypto : PersistentSingleton<Crypto> {
        private const string CryptoKey =
            "Deplorable Mountaineer Encryption Key for Framework 2D correct horse battery staple";

        private static string _userKey;

        //While an app specific salt is not the best practice for
        //password based encryption, it's probably safe enough as long as
        //it is truly uncommon. Also too much work to alter this answer otherwise.
        private static readonly byte[] Salt = {
            31, 41, 59, 26, 53, 58, 27, 182, 81, 82, 84, 59, 45, (byte) 'D', (byte) 'e',
            (byte) 'p', (byte) 'l', (byte) 'o', (byte) 'r', (byte) 'a', (byte) 'b', (byte) 'l',
            (byte) 'e', (byte) ' ', (byte) 'M', (byte) 'o', (byte) 'u', (byte) 'n', (byte) 't',
            (byte) 'a', (byte) 'i', (byte) 'n', (byte) 'e', (byte) 'e', (byte) 'r', (byte) 'F',
            (byte) 'r', (byte) 'a', (byte) 'm', (byte) 'e', (byte) 'w', (byte) 'o', (byte) 'r',
            (byte) 'k', (byte) '2', (byte) 'D'
        };

        [SerializeField] [ReadOnly] private string applicationKey;

        protected override void Awake(){
            base.Awake();
            SetUserKey();
        }

        private void Reset(){
            applicationKey = Guid.NewGuid().ToString();
            SetUserKey(true);
        }

        public static void SetUserKey(bool forceRegenerate = false){
            string dir = Application.persistentDataPath +
                         Path.DirectorySeparatorChar +
                         "ID";
            string file = dir + Path.DirectorySeparatorChar + "_UserId";
            string userIdKey = "USER_ID";

            bool regenerateKey = !PlayerPrefs.HasKey(userIdKey) && !File.Exists(file) ||
                                 forceRegenerate;
            if(regenerateKey){
                Debug.Log("Regenerating user key");
                if(Directory.Exists(dir)) Directory.Delete(dir, true);
                PlayerPrefs.DeleteAll();
                _userKey = Guid.NewGuid().ToString();
            }
            else if(PlayerPrefs.HasKey(userIdKey)){
                _userKey = PlayerPrefs.GetString(userIdKey);
            }
            else{
                Debug.Assert(File.Exists(file));
                StreamReader reader = new StreamReader(file);
                _userKey = reader.ReadToEnd();
                reader.Close();
            }

            if(!File.Exists(file)){
                if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                StreamWriter writer = new StreamWriter(file, false);
                writer.Write(_userKey);
                writer.Close();
            }

            if(PlayerPrefs.HasKey(userIdKey)) return;
            PlayerPrefs.SetString(userIdKey, _userKey);
            PlayerPrefs.Save();
        }

        private static string GetEncryptionKey(string password = CryptoKey){
            if(string.IsNullOrEmpty(_userKey)) SetUserKey();
            string encryptionKey = KeyHash(_userKey, password);
            if(!string.IsNullOrEmpty(Instance.applicationKey))
                encryptionKey = KeyHash(encryptionKey, Instance.applicationKey);
            return encryptionKey;
        }

        /// <summary>
        ///     Save a serializable object with encryption
        /// </summary>
        /// <param name="obj">serializable object</param>
        /// <param name="file">path to save in</param>
        /// <param name="password">override default password</param>
        public static void SaveObjectToFile(object obj, string file,
            string password = CryptoKey){
            WriteToFile(JsonUtility.ToJson(obj), file, password);
        }

        /// <summary>
        ///     Restore a saved, encrypted serializable object
        /// </summary>
        /// <param name="file">path to load from</param>
        /// <param name="password">override default password</param>
        /// <returns>serializable object</returns>
        public static T LoadObjectFromFile<T>(string file, string password = CryptoKey){
            return JsonUtility.FromJson<T>(ReadFromFile(file, password));
        }

        /// <summary>
        ///     Restore a saved, encrypted string
        /// </summary>
        /// <param name="file">Path to load from</param>
        /// <param name="password">override default password</param>
        /// <returns>The string</returns>
        public static string ReadFromFile(string file, string password = CryptoKey){
            StreamReader reader = new StreamReader(file);
            string encrypted = reader.ReadToEnd();
            reader.Close();
            return DecryptStringAes(encrypted, password);
        }

        /// <summary>
        ///     Save a string with encryption
        /// </summary>
        /// <param name="contents">The string to save</param>
        /// <param name="file">Path to save to</param>
        /// <param name="password">override default password</param>
        public static void WriteToFile(string contents, string file,
            string password = CryptoKey){
            string encrypted = EncryptStringAes(contents, password);
            StreamWriter writer = new StreamWriter(file, false);
            writer.Write(encrypted);
            writer.Close();
        }

        /// <summary>
        ///     Return true if encrypted key exists in player prefs
        /// </summary>
        /// <param name="key">The key to query</param>
        /// <param name="password">override default password</param>
        /// <returns>True or false</returns>
        public static bool HasKey(string key, string password = CryptoKey){
            string encryptedKey = KeyHash(key, password);
            return PlayerPrefs.HasKey(encryptedKey);
        }

        /// <summary>
        ///     Return a string stored with encryption in player prefs
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="password">override default password</param>
        /// <returns>The string</returns>
        public static string GetString(string key, string password = CryptoKey){
            string encryptedKey = KeyHash(key, password);
            if(!PlayerPrefs.HasKey(encryptedKey)) return "";
            string encryptedVal = PlayerPrefs.GetString(encryptedKey);
            return DecryptStringAes(encryptedVal, password);
        }

        /// <summary>
        ///     Store a string encrypted in player prefs
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="val">The string to save</param>
        /// <param name="password">override default password</param>
        public static void SetString(string key, string val,
            string password = CryptoKey){
            string encryptedKey = KeyHash(key, password);
            string encryptedVal = EncryptStringAes(val, password);
            PlayerPrefs.SetString(encryptedKey, encryptedVal);
        }

        /// <summary>
        ///     Return an integer stored with encryption in player prefs
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="password">override default password</param>
        /// <returns>The integer</returns>
        public static int GetInt(string key, string password = CryptoKey){
            string str = GetString(key, password);
            return string.IsNullOrEmpty(str) ? 0 : int.Parse(str);
        }

        /// <summary>
        ///     Store an integer encrypted in player prefs
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="val">The integer to save</param>
        /// <param name="password">override default password</param>
        public static void SetInt(string key, int val,
            string password = CryptoKey){
            SetString(key, val.ToString(), password);
        }

        /// <summary>
        ///     Return a floating point number stored with encryption in player prefs
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="password">override default password</param>
        /// <returns>The float</returns>
        public static float GetFloat(string key, string password = CryptoKey){
            string str = GetString(key, password);

            return string.IsNullOrEmpty(str)
                ? 0
                : float.Parse(str, CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Store a floating point number encrypted in player prefs
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="val">The float to save</param>
        /// <param name="password">override default password</param>
        public static void SetFloat(string key, float val,
            string password = CryptoKey){
            SetString(key, val.ToString(CultureInfo.InvariantCulture), password);
        }

        /// <summary>
        ///     "Encrypt" the key with a one-to-one (not surjective) hash
        /// </summary>
        /// <param name="key">The key to hash</param>
        /// <param name="password">override default password</param>
        /// <returns></returns>
        private static string KeyHash(string key, string password = CryptoKey){
            string result = "";
            for(int i = 0; i < Mathf.Max(key.Length, password.Length); i++){
                int c = key[i%key.Length]*127 + password[i%password.Length]*113;
                result += Convert.ToBase64String(BitConverter.GetBytes(c));
            }

            return result;
        }

        /// <summary>
        ///     Encrypt the given string using AES.  The string can be decrypted using
        ///     DecryptStringAES().  The sharedSecret parameters must match.
        /// </summary>
        /// <param name="plainText">The text to encrypt.</param>
        /// <param name="sharedSecret">A password used to generate a key for encryption.</param>
        /// <param name="additionalKeys"></param>
        public static string EncryptStringAes(string plainText,
            string sharedSecret = CryptoKey, bool additionalKeys = true){
            if(string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));
            if(string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException(nameof(sharedSecret));

            string outStr; // Encrypted string to return
            RijndaelManaged aesAlg = null; // RijndaelManaged object used to encrypt the data.

            try{
                // generate the key from the shared secret and the salt

                Rfc2898DeriveBytes key = additionalKeys
                    ? new Rfc2898DeriveBytes(GetEncryptionKey(sharedSecret), Salt)
                    : new Rfc2898DeriveBytes(sharedSecret, Salt);

                // Create a RijndaelManaged object
                aesAlg = new RijndaelManaged();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize/8);
                //  Debug.Log(plainText + " :: " + Convert.ToBase64String(aesAlg.Key));
                // Create a decryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using MemoryStream msEncrypt = new MemoryStream();
                // prepend the IV
                msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                using(CryptoStream csEncrypt =
                    new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)){
                    using(StreamWriter swEncrypt = new StreamWriter(csEncrypt)){
                        //Write all data to the stream.
                        swEncrypt.Write(plainText);
                    }
                }

                outStr = Convert.ToBase64String(msEncrypt.ToArray());
            }
            finally{
                // Clear the RijndaelManaged object.
                aesAlg?.Clear();
            }

            // Return the encrypted bytes from the memory stream.
            return outStr;
        }

        /// <summary>
        ///     Decrypt the given string.  Assumes the string was encrypted using
        ///     EncryptStringAES(), using an identical sharedSecret.
        /// </summary>
        /// <param name="cipherText">The text to decrypt.</param>
        /// <param name="sharedSecret">A password used to generate a key for decryption.</param>
        /// <param name="additionalKeys"></param>
        public static string DecryptStringAes(string cipherText,
            string sharedSecret = CryptoKey, bool additionalKeys = true){
            if(string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));
            if(string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException(nameof(sharedSecret));

            // Declare the RijndaelManaged object
            // used to decrypt the data.
            RijndaelManaged aesAlg = null;

            // Declare the string used to hold
            // the decrypted text.
            string plaintext;

            try{
                // generate the key from the shared secret and the salt
                Rfc2898DeriveBytes key = additionalKeys
                    ? new Rfc2898DeriveBytes(GetEncryptionKey(sharedSecret), Salt)
                    : new Rfc2898DeriveBytes(sharedSecret, Salt);


                // Create the streams used for decryption.                
                byte[] bytes = Convert.FromBase64String(cipherText);
                using MemoryStream msDecrypt = new MemoryStream(bytes);
                // Create a RijndaelManaged object
                // with the specified key and IV.
                aesAlg = new RijndaelManaged();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize/8);
                // Get the initialization vector from the encrypted stream
                aesAlg.IV = ReadByteArray(msDecrypt);
                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using CryptoStream csDecrypt =
                    new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using StreamReader srDecrypt = new StreamReader(csDecrypt);
                plaintext = srDecrypt.ReadToEnd();
            }
            finally{
                // Clear the RijndaelManaged object.
                aesAlg?.Clear();
            }

            return plaintext;
        }

        private static byte[] ReadByteArray(Stream s){
            byte[] rawLength = new byte[sizeof(int)];
            if(s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
                throw new SystemException(
                    "Stream did not contain properly formatted byte array");

            byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
            if(s.Read(buffer, 0, buffer.Length) != buffer.Length)
                throw new SystemException("Did not read byte array properly");

            return buffer;
        }
    }
}