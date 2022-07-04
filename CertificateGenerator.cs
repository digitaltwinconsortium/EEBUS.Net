using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace EEBUS
{
    public class CertificateGenerator
    {
        public static X509Certificate2 GenerateCert(string subject)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), subject + ".pfx");
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
                // generate a new cert request with ECC and NIST curve P256 (TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256, EEBUS requirement)
                CertificateRequest request = new CertificateRequest("cn=" + subject, ECDsa.Create(ECCurve.NamedCurves.nistP256), HashAlgorithmName.SHA256);

                // add Subject Key Identifier (EEBUS requirement)
                request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

                // add DNS name and localhost IP address also as Subject Alternate Name
                SubjectAlternativeNameBuilder subjectAlternativeNameBuilder = new SubjectAlternativeNameBuilder();
                subjectAlternativeNameBuilder.AddDnsName(subject);
                request.CertificateExtensions.Add(subjectAlternativeNameBuilder.Build());

                // add key usage
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

                // create cert
                X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddSeconds(-5), DateTimeOffset.UtcNow.AddYears(1));

                // persist the cert
                File.WriteAllBytes(path, cert.Export(X509ContentType.Pfx, password));

                return cert;
            }
        }
    }
}
