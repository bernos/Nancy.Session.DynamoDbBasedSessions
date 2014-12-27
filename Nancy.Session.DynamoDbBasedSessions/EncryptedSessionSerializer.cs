using System;
using System.Collections.Generic;
using System.Text;
using Nancy.Cryptography;
using Nancy.Helpers;

namespace Nancy.Session
{
    public class EncryptedSessionSerializer : ISessionSerializer
    {
        public CryptographyConfiguration CryptographyConfiguration { get; set; }
        public IObjectSerializer Serializer { get; set; }

        public EncryptedSessionSerializer()
        {
            CryptographyConfiguration = CryptographyConfiguration.Default;
            Serializer = new DefaultObjectSerializer();
        }

        public ISession Deserialize(string data)
        {
            data = Decrypt(data);

            var dictionary = new Dictionary<string, object>();

            if (!String.IsNullOrEmpty(data))
            {
                var tokens = data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var token in tokens)
                {
                    var rawKey = token.Substring(0, token.IndexOf('='));
                    var rawValue = token.Substring(1 + token.IndexOf('='));

                    var value = Serializer.Deserialize(HttpUtility.UrlDecode(rawValue));
                    dictionary[HttpUtility.UrlDecode(rawKey)] = value;
                }
            }

            return new Session(dictionary);
        }

        public string Serialize(ISession session)
        {
            var sb = new StringBuilder();

            foreach (var kvp in session)
            {
                var objectString = Serializer.Serialize(kvp.Value);

                sb.Append(HttpUtility.UrlEncode(kvp.Key));
                sb.Append("=");
                sb.Append(objectString);
                sb.Append(";");
            }

            return Encrypt(sb.ToString());
        }


        private string Encrypt(string clearText)
        {
            var cryptographyConfiguration = CryptographyConfiguration;
            var encryptedData = cryptographyConfiguration.EncryptionProvider.Encrypt(clearText);
            var hmacBytes = cryptographyConfiguration.HmacProvider.GenerateHmac(encryptedData);

            return string.Format("{0}{1}", Convert.ToBase64String(hmacBytes), encryptedData);
        }

        private string Decrypt(string cypherText)
        {
            var cryptographyConfiguration = CryptographyConfiguration;
            var hmacLength = Base64Helpers.GetBase64Length(cryptographyConfiguration.HmacProvider.HmacLength);
            var hmacString = cypherText.Substring(0, hmacLength);
            var encryptedData = cypherText.Substring(hmacLength);
            var hmacBytes = Convert.FromBase64String(hmacString);
            var newHmac = cryptographyConfiguration.HmacProvider.GenerateHmac(encryptedData);
            var isHmacValid = HmacComparer.Compare(newHmac, hmacBytes, cryptographyConfiguration.HmacProvider.HmacLength);

            return isHmacValid ? cryptographyConfiguration.EncryptionProvider.Decrypt(encryptedData) : string.Empty;
        }
    }
}