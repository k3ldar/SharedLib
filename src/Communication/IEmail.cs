/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2022 Simon Carter
 *
 *  Purpose:  Email Send Class
 *
 */
namespace Shared.Communication
{
    public interface IEmail
    {
        string Host { get; set; }
        string Password { get; set; }
        int Port { get; set; }
        string Sender { get; set; }
        bool SSL { get; set; }
        string User { get; set; }
        bool SendEmail(string senderName, string recipientName, string recipientEmail, string message, string subject, bool isHtml, params string[] attachments);
        bool SendTestEmail();
    }
}