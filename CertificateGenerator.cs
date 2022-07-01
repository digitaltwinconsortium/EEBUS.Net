using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace EEBUS
{
    public class CertificateGenerator
    {
        public static X509Certificate2 GenerateCert()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "EEBUSCert.pfx");
            string password = Environment.GetEnvironmentVariable("PFX_PASSWORD");

            if (password == null)
            {
                password = string.Empty;
            }

            // check if we have a persisted cert already
            if (File.Exists(path))
            {
                // load and return
                return new X509Certificate2(path);
            }
            else
            {
                // generate a new cert request
                CertificateRequest request = new CertificateRequest("cn=localhost&127.0.0.1", RSA.Create(2048), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // add Subject Key Identifier (EEBUS requirement)
                request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

                // add DNS name and localhost IP address also as Subject Alternate Name
                SubjectAlternativeNameBuilder subjectAlternativeNameBuilder = new SubjectAlternativeNameBuilder();
                subjectAlternativeNameBuilder.AddDnsName("localhost");
                subjectAlternativeNameBuilder.AddIpAddress(IPAddress.Parse("127.0.0.1"));
                request.CertificateExtensions.Add(subjectAlternativeNameBuilder.Build());

                // add key usage
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

                // add enhanced key usage
                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("2.5.29.32.0"), new Oid("1.3.6.1.5.5.7.3.1") }, false));

                // create cert
                X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddSeconds(-5), DateTimeOffset.UtcNow.AddYears(1));

                // persist the cert
                File.WriteAllBytes(path, cert.Export(X509ContentType.Pfx, password));

                return cert;
            }
        }
    }
}
