using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shared.Communication;

#if DEBUG

namespace SharedLibTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class ReceivedEmailTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_InvalidRawData_Null_Throws_ArgumentNullException()
        {
            new ReceivedEmail(null);
        }

        [TestMethod]
        public void Validate_GMailMail_Mime_Success()
        {
            ReceivedEmail sut = new ReceivedEmail(new StringBuilder(Properties.Resources.GMailMail));

            Assert.IsNotNull(sut);
            Assert.AreEqual(3, sut.ContentType.Count);

            Assert.IsNotNull(sut.ContentType[0].Boundary);
            Assert.AreEqual("00000000000066df2505e953716d", sut.ContentType[0].Boundary);
            Assert.IsNull(sut.ContentType[0].CharSet);
            Assert.AreEqual("multipart/alternative", sut.ContentType[0].Content);
            Assert.AreEqual(6, sut.ContentType[0].Index);
            Assert.IsFalse(sut.ContentType[0].IsHtmlText);
            Assert.IsTrue(sut.ContentType[0].IsMultiPart);
            Assert.IsFalse(sut.ContentType[0].IsPlainText);

            Assert.IsNull(sut.ContentType[1].Boundary);
            Assert.AreEqual("UTF-8", sut.ContentType[1].CharSet);
            Assert.AreEqual("text/plain", sut.ContentType[1].Content);
            Assert.AreEqual(9, sut.ContentType[1].Index);
            Assert.IsFalse(sut.ContentType[1].IsHtmlText);
            Assert.IsFalse(sut.ContentType[1].IsMultiPart);
            Assert.IsTrue(sut.ContentType[1].IsPlainText);

            Assert.IsNull(sut.ContentType[2].Boundary);
            Assert.AreEqual("UTF-8", sut.ContentType[2].CharSet);
            Assert.AreEqual("text/html", sut.ContentType[2].Content);
            Assert.AreEqual(14, sut.ContentType[2].Index);
            Assert.IsTrue(sut.ContentType[2].IsHtmlText);
            Assert.IsFalse(sut.ContentType[2].IsMultiPart);
            Assert.IsFalse(sut.ContentType[2].IsPlainText);

            Assert.AreEqual("Fri, 23 Sep 2022 09:51:16 +0200", sut.Date);
            Assert.AreEqual("Si Carter <s1cart3r@gmail.com>", sut.From);
            Assert.IsNotNull(sut.HtmlMessage);
            Assert.IsNotNull(sut.TextMessage);
            Assert.IsTrue(sut.IsValid);
            Assert.AreEqual(2, sut.MailContent.Count);

            Assert.AreEqual(1, sut.MailContent[0].Headers.Count);
            Assert.AreEqual(2, sut.MailContent[1].Headers.Count);
            Assert.AreEqual("<CANY3bHNCowtzhX7RwD0g5_Xsv66s59O-X0-QuUNcGxLByB4wmA@mail.gmail.com>", sut.MessageId);
            Assert.AreEqual("1.0", sut.MimeVersion);
            Assert.AreEqual("sonar cloud", sut.Subject);
            Assert.IsNotNull(sut.TextMessage);
            Assert.AreEqual("s1cart3r@gmail.com", sut.To);
            Assert.AreEqual(23, sut.Fields.Count);
        }

        [TestMethod]
        public void Validate_Outlook_Mime_Success()
        {
            ReceivedEmail sut = new ReceivedEmail(new StringBuilder(Properties.Resources.outlookmail));

            Assert.IsNotNull(sut);
            Assert.AreEqual(3, sut.ContentType.Count);

            int idx = 0;
            Assert.AreEqual("=-TTHoVzIZsVFeE7JogSUalQ==", sut.ContentType[idx].Boundary);
            Assert.IsNull(sut.ContentType[idx].CharSet);
            Assert.AreEqual("multipart/alternative", sut.ContentType[idx].Content);
            Assert.AreEqual(84, sut.ContentType[idx].Index);
            Assert.IsFalse(sut.ContentType[idx].IsHtmlText);
            Assert.IsTrue(sut.ContentType[idx].IsMultiPart);
            Assert.IsFalse(sut.ContentType[idx].IsPlainText);
            Assert.IsFalse(sut.ContentType[idx].IsUtf8);

            idx++;
            Assert.IsNull(sut.ContentType[idx].Boundary);
            Assert.AreEqual("utf-8", sut.ContentType[idx].CharSet);
            Assert.AreEqual("text/plain", sut.ContentType[idx].Content);
            Assert.AreEqual(208, sut.ContentType[idx].Index);
            Assert.IsFalse(sut.ContentType[idx].IsHtmlText);
            Assert.IsFalse(sut.ContentType[idx].IsMultiPart);
            Assert.IsTrue(sut.ContentType[idx].IsPlainText);
            Assert.IsTrue(sut.ContentType[idx].IsUtf8);

            idx++;
            Assert.IsNull(sut.ContentType[idx].Boundary);
            Assert.AreEqual("utf-8", sut.ContentType[idx].CharSet);
            Assert.AreEqual("text/html", sut.ContentType[idx].Content);
            Assert.AreEqual(226, sut.ContentType[idx].Index);
            Assert.IsTrue(sut.ContentType[idx].IsHtmlText);
            Assert.IsFalse(sut.ContentType[idx].IsMultiPart);
            Assert.IsFalse(sut.ContentType[idx].IsPlainText);
            Assert.IsTrue(sut.ContentType[idx].IsUtf8);

            Assert.AreEqual("Tue, 01 Nov 2022 10:47:15 -0700", sut.Date);
            Assert.AreEqual(267, sut.Fields.Count);
            Assert.AreEqual("Microsoft account team <account-security-noreply@accountprotection.microsoft.com>", sut.From);
            Assert.IsTrue(sut.IsValid);
            Assert.AreEqual("<6WGRCU3EAIU4.Y0KWJ5TANI5R@BY1PEPF00001B91>", sut.MessageId);
            Assert.AreEqual("1.0", sut.MimeVersion);
            Assert.AreEqual("Microsoft account security info was added", sut.Subject);
            Assert.AreEqual("netcorepluginmanager@outlook.com", sut.To);

            Assert.IsNotNull(sut.HtmlMessage);
            Assert.AreEqual(24607, sut.HtmlMessage.Length);
            Assert.IsNotNull(sut.TextMessage);
            Assert.AreEqual(543, sut.TextMessage.Length);

            Assert.AreEqual(2, sut.MailContent.Count);

            Assert.AreEqual(2, sut.MailContent[0].Headers.Count);
            Assert.IsTrue(sut.MailContent[0].Message.StartsWith("The following security info was recently added to the Microsoft"));

            Assert.AreEqual(2, sut.MailContent[1].Headers.Count);
            Assert.IsTrue(sut.MailContent[1].Message.StartsWith("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">"));
        }

        [TestMethod]
        public void Validate_Other_Success()
        {
            ReceivedEmail sut = new ReceivedEmail(new StringBuilder(Properties.Resources.OtherMail));

            Assert.IsNotNull(sut);
            Assert.AreEqual(1, sut.ContentType.Count);

            Assert.IsNull(sut.ContentType[0].Boundary);
            Assert.AreEqual("iso-8859-1", sut.ContentType[0].CharSet);
            Assert.AreEqual("text/html", sut.ContentType[0].Content);
            Assert.AreEqual(77, sut.ContentType[0].Index);
            Assert.IsTrue(sut.ContentType[0].IsHtmlText);
            Assert.IsTrue(sut.ContentType[0].IsLatin);
            Assert.IsFalse(sut.ContentType[0].IsMultiPart);
            Assert.IsFalse(sut.ContentType[0].IsPlainText);
            Assert.IsFalse(sut.ContentType[0].IsUtf16);
            Assert.IsFalse(sut.ContentType[0].IsUtf32);
            Assert.IsFalse(sut.ContentType[0].IsUtf7);
            Assert.IsFalse(sut.ContentType[0].IsUtf8);

            Assert.AreEqual("info@nordnet.dk", sut.From);
            Assert.IsNotNull(sut.HtmlMessage);
            Assert.IsTrue(sut.HtmlMessage.StartsWith("Sidste svardag p=E5 dette tilbud 2022-11-08. <br/><br/> Hej!<br/> <br/>Vi h=\r\nar genbestilt dine tegningsretter i den"));
            Assert.IsTrue(sut.IsValid);
            Assert.AreEqual(1, sut.MailContent.Count);
            Assert.AreEqual("<63c5f3$2kpon6@mail1.prod.nordnet.se>", sut.MessageId);
            Assert.AreEqual("1.0", sut.MimeVersion);
            Assert.AreEqual("Nyemission Meltron", sut.Subject);
            Assert.IsNull(sut.TextMessage);
            Assert.AreEqual("s1cart3r@gmail.com", sut.To);
            Assert.AreEqual(114, sut.Fields.Count);
        }
    }
}

#endif