/*
 * This file is part of the Bucket package.
 *
 * (c) Yu Meng Han <menghanyu1994@gmail.com>
 *
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 *
 * Document: https://github.com/getbucket/bucket/wiki
 */

using Bucket.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bucket.Tests.Util
{
    [TestClass]
    public class TestsSecurity
    {
        private readonly string publicKey =
@"-----BEGIN PUBLIC KEY-----
MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAvXOaKfHbqjXUYPEJWqnv
8OA+8qz7gOvaro6db6tURvQg1YL+nSBw+iCIrbNVODyi3JGA6muW5A7SczDXuyJ9
9rwwpE43DVqkVvZm2yjy3XkogxRU7k+xUOatYZiMWthUY/+znSk1AYVJsm8n7vqG
hm2xGYWrpHy+INBKkDE4FLhpThqr63LssZD9tpUbM1m00sUVWXQzYUFeqaQFzdWC
cYRqMDeyEXY11emTLnu9PrzSPzzbuXbKv0kkYdG3uIWsHaq3rXelXIDzXmlEU5Gb
WXlplw7SNKq0PEwmmkuC+pUMfFfc3D8bmve+9JB7HodWGCYf7M460MTDiTZiUPRu
XD34tUeSf0OMT+vXyktLT2eTpChBUReTBQOdqag+1Y8kUMpslaVxIUGVA1fdV/wO
L9aY292qsJdIeXUbNMnTb5uzXAHtbYQ1IzaIMUf+/JD3urXQWeo+yHYeHChl+YNf
vVFEiDl/7v2573koe/jkd1sNBlaUSFbbW2L2y/Xnx1gJxpHNWFyyNXkOTWhBhzu2
okb7Lq9I4DiPA9Vd5GdIDc/jLQ/8iCaiC5089aJu+NXduj35LrLztelCZySclcJn
PFiGsVhSLCn8gtrAQ1JvI1AelYFCe4wkBUFL4imRTqR+h7IRPiBVEcrhqN6lRwNU
jS3/O4+CBCYlgS80ua/CVHkCAwEAAQ==
-----END PUBLIC KEY-----
";

        [TestMethod]
        [DataRow("acbd18db4cc2f85cedef654fccc4a4d8", "foo", false)]
        [DataRow("37B51D194A7513E45B56F6524F2D51F2", "bar", true)]
        public void TestMd5(string expected, string content, bool uppercase)
        {
            Assert.AreEqual(expected, Security.Md5(content, uppercase));
        }

        [TestMethod]
        public void TestVerifySignatureRSA384()
        {
            var expctedSignature = @"b4TSWqFR53R2b0ox5sPc/KFnjJoXqfKRx9k8Vh8mMBZaMqKfLx6f3HIdOrpTxI4wpXc300q55NMIJh+i5wvEoy998WaP8CTAc6hHLQdFyITRo3M6uTQGEXGRntLwQyufTjp7wM9r5wqWWQ8n53URkGKomy+MCfAkB18zCZoFAFXBjndYIdMkgZTBz7jA53rSv2ZTrOps43wNAAPJWDcEJAd2AbUmyXLQqvOf978tWPwqR/ZUc24tjLsR49mcZ8ajkAaxezFjySGml8quHgVBHkgajGWp+z0pgDq0xx+33KMK4QdUy2PoUGxGfi9IkM+nzSdzMh5HhwVD5/1yOhOVp/qVvqKJsEn40Ue2UptsS6yZrzW38CeKG/NVvwasTOBdB3bDQuy0yxQbKOsTXFNFIHltT/Xdo0usGXFke8gyIuZgt16o2554NJLDKelPIHRwqdkn1pS36c0L1XhVBNakABam2mDnCWoJppdnBDQ1Y59clBHoIuzfdTkZ7I7j4YSNHzf1/VGPvGMJRZMtWkkmxAsE6r6aV3osOhfLJqZzr36K9w166a0qYQ+/VHS7pBHJPIQL5UObcH4DnQk3K9CrsCwnmdQNkCF9BtxXk75avb+rfor9DriPPLb09v1uh8BfBYMdI6hNJXObx1yQj1465vWpNq31AdcjIjqDi98hLjE=";
            var data = "foobarbaz".ToStream();
            Assert.IsTrue(Security.VerifySignature(data, expctedSignature, publicKey));
        }

        [TestMethod]
        public void TestVerifySignatureRSA256()
        {
            var expctedSignature = @"JYW3cRtNjlWzcioj+68BRlWhsHFPVqBakjBRj29ZkWFJJaVzjVIjIkh4GluGKqI0qsOx0Z/3ATRL8EbT/p37k2yzdvNxKs6m+HQlNkTaV2hQCt2LLaxl5g3adpEQkoPGehPSOE/wd7BJZEl81iVytwGfxaLEiT8pGkqDQ+4lDWXR+PcQRt46y/qPSqNQxbGAaVYMjF9gC5nKUMB5QxCTPMqxO0himOnH7llijWovFOThZdV7tMHN3+X5dIlBeQ2OPpOsv2T2+VyeSfha4fmGwjLPTzKqM430wR+3AF1lbjdVqIF1MHDH0AMEv4tn7NFJ1ngc5kBnhagFttvc3GKROuQkHoVu9bbkxOBouoq7eW9Kup1GNeq336ci0ZwDF8D6D7tVNvv9CwfzQBP5QIhP8Tfl1XZh3XrDfpKLZqIfL0/wvN3w28KLOcd53gzxLCntpZJK02Ms+P7wLfaVStcbVXxmaXUvugvkSHRetlUiRcTnPr/vBPmtwAXxAfwezX53B7d9BaSZdlAV33NJzOdvaS6TQ6M4XKt9J7onW0a3k52Q9Mw4LH+SNpXtGW9DzoNWNH+lryTBx53bOtcbIY8JWZ7iMrO2yQ0tmcpCiKwGVpGMU/y6VXXnpbok1JMiimEm1gYptt9cJAgpoUd6eXHgQWQi9UH9nS5DhYI+6mYMuvs=";
            var data = "foobarbaz".ToStream();
            Assert.IsTrue(Security.VerifySignature(data, expctedSignature, publicKey, "SHA256"));
        }

        [TestMethod]
        public void TestVerifySignatureRSA512()
        {
            var expctedSignature = @"a8ZoQDFIswJLdwGe9f2dOp64143PSWUgESn5GKnOXF9RGrJlx4yn31HzL3uSVcpQeFDdLtHDlsP7ssZt6z59gmxbRg487Lt02TrjdZhJFvCUgeLKcWfBjmIBf5bF88u5Tq2E2NTBQd32rY5yQuGCq9+ZNamHzALA3ds9zg4N6TotbzzlPJxxf3GjwVAkjInKWz0UwQzOMDKM9FXjmqGphHjyIzeTv8TfzyqM5B8u4XYVFAnnypDL20D4zim6vAnADCkQwIVeBo3DFhcJnWGoEcYiI3d7tUalqzSU63kYEy+zmD2G5UQwyJ8VL5Gw+g81wReOkKuBK1XqvW7LaF8a9SRhDJ2YvEaltJHYOA8HEmxArlhfFL3OKnMjeP1TBGlvVDUhAEnDrBlZfhKHB7D2Q3uqpFGSNsu7u/nHooOlBgHVvct2Z7Vx2+AVGZe8RasGis6HIfy/llFl5+Rg4VazgiJU70YsiQB7VBGEb1dgKpG62yQfripINYSIVE57Fdo96n18CMk9qTJYXni/JuYdmQOuxRyoZRwRADboqHxxGvSH20b1ViIlJYEmIfpw0Nl90n9okR1MT+n2xTW1il7DNXhBG4/VZDY2Ro39yrN6OjOx4UGOTBNxOwI7jfZVy92rB2F1ZPQeRnmWAupTFLqotRP6PXssj7X8h+3vCbcI29U=";
            var data = "foobarbaz".ToStream();
            Assert.IsTrue(Security.VerifySignature(data, expctedSignature, publicKey, "SHA512"));
        }

        [TestMethod]
        public void TestVerifySigatureLargeContentSameSignatureLength()
        {
            var expctedSignature = @"WSEC2paEhBzDteTsLm3+UfAj9j2odnYRcoOOnkczGRAnSkKh17nqLRU+SU9ouTWSI7WnoOURZrnL+ooWDbbYjzbfGVCwb75gqYWQa+Z82O5Rkl0X38o9bw+06cmKBzl8gbDmPtc4FtICw4ujEO90ibgXpHfoWflGtba51BOJTj23MdAD+7G4jP+VtlTOYm3NpV3Syxgk6vNp+xLv4ByLPCf0EfBVH+QURFbGLTm/BMPfZBMrtYVCDagvCg2Kr4WAGUPk2vRUuCKTOw2NlWb4d6msy6ZqZ80U0osCQ6EGf7tLnLAfVBbq6me3Tr04V4oP9Qa/Wof0ulVQcKpuGAqZM/8PRKROoT3SdGbR47WctoMio+gwg5hb/iBk8AaAQp+zzCMej9Pc3DuY5Yx7aK5i5Dr3Vm73ZqOK8SKRWpaCQZfEm3Z+wF9kih4Q8xZmAbQgMw0eb9rUO/quT2b3MNDAXk82robypHqzbH1l9nIxCW4dF8z1tnBRoOyv4nDwiSNH0HwFRwJOR7NPhixW6IsPOguIGtOuMuYtOLxyWOs4UfIja+S7U1oXVYCEL1IlpcDT+xAlwd9LmyolxFIfp7LAKVHmkjQ/GX31oM/I8bQi/159D+dr0rhjGsofKOXktt596nYHhmgVTAtB1lvXu2l37rpxa/5FZPUloIgpFRIVS6Y=";
            var data = "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789".ToStream();
            Assert.IsTrue(Security.VerifySignature(data, expctedSignature, publicKey, "SHA512"));
        }
    }
}
