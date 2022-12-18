/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2022 Simon Carter
 *
 *  Purpose:  Received email class
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if NET5

namespace Shared.Communication
{
    public sealed class ReceivedEmail
    {
        private readonly List<string> _fields;

        public ReceivedEmail(StringBuilder rawMail)
        {
            if (rawMail == null)
                throw new ArgumentNullException(nameof(rawMail));

            _fields = new List<string>();
            InternalInitialParseEmail(rawMail);

            ContentType = new();
            ProcessHeaders();

            MailContent = new();
            ProcessContent();
        }

        public string MimeVersion { get; private set; }

        public string Date { get; private set; }

        public string MessageId { get; private set; }

        public string Subject { get; private set; }

        public string From { get; private set; }

        public string To { get; private set; }

        public List<ContentTypeData> ContentType { get; private set; }

        public List<MailContent> MailContent { get; private set; }

        public string TextMessage { get; private set; }

        public string HtmlMessage { get; private set; }

        public bool IsValid => !String.IsNullOrEmpty(TextMessage) || !String.IsNullOrEmpty(HtmlMessage);

        public IReadOnlyList<string> Fields => _fields;

        private void ProcessContent()
        {
            // get text/html messages from fields using content type
            if (ContentType.Count == 0)
                return;

            if (ContentType.Count == 1)
            {
                int idx = ContentType[0].Index;

                // find starting line
                for (int j = _fields.Count - 1; j >= 0; j--)
                {
                    if (String.IsNullOrEmpty(_fields[j]))
                    {
                        idx = j + 1;
                        break;
                    }
                }

                StringBuilder content = new((_fields.Count - idx) * 900);

                for (int k = idx; k < _fields.Count; k++)
                {
                    content.AppendLine(_fields[k]);
                }

                MailContent.Add(new MailContent(content, ContentType[0]));
            }

            if (ContentType[0].IsMultiPart)
            {
                for (int i = 1; i < ContentType.Count; i++)
                {
                    int idx = ContentType[i].Index;

                    // find starting line
                    for (int j = idx; j >= 0; j--)
                    {
                        if (_fields[j].StartsWith($"--{ContentType[0].Boundary}", StringComparison.InvariantCultureIgnoreCase))
                        {
                            idx = j + 1;
                            break;
                        }
                    }

                    StringBuilder content = new((_fields.Count - idx) * 900);

                    for (int k = idx; k < _fields.Count; k++)
                    {
                        if (_fields[k].StartsWith($"--{ContentType[0].Boundary}", StringComparison.InvariantCultureIgnoreCase))
                            break;

                        content.AppendLine(_fields[k]);
                    }

                    MailContent.Add(new MailContent(content));
                }
            }

            MailContent plain = MailContent.FirstOrDefault(mc => mc.Headers.Any(h => h.IsPlainText));
            TextMessage = plain?.Message;

            MailContent html = MailContent.FirstOrDefault(mc => mc.Headers.Any(h => h.IsHtmlText));
            HtmlMessage = html?.Message;
        }

        private void ProcessHeaders()
        {
            for (int i = 0; i < _fields.Count; i++)
            {
                string field = _fields[i];

                if (String.IsNullOrEmpty(field))
                    continue;

                if (field.StartsWith("MIME-Version:", StringComparison.InvariantCultureIgnoreCase))
                    MimeVersion = GetFieldValue(field, 14);
                else if (field.StartsWith("Date:", StringComparison.InvariantCultureIgnoreCase))
                    Date = GetFieldValue(field, 5);
                else if (field.StartsWith("Message-ID:", StringComparison.InvariantCultureIgnoreCase))
                    MessageId = GetFieldValue(field, 12);
                else if (field.StartsWith("Subject:", StringComparison.InvariantCultureIgnoreCase))
                    Subject = GetFieldValue(field, 8);
                else if (field.StartsWith("From:", StringComparison.InvariantCultureIgnoreCase))
                    From = GetFieldValue(field, 5).Replace("\t", " ");
                else if (field.StartsWith("To:", StringComparison.InvariantCultureIgnoreCase))
                    To = GetFieldValue(field, 3);
                else if (field.StartsWith("Content-Type:", StringComparison.InvariantCultureIgnoreCase))
                    ContentType.Add(new ContentTypeData(GetFieldValue(field, 13), i));
            }
        }

        private string GetFieldValue(string field, int startPosition)
        {
            if (String.IsNullOrEmpty(field))
                return String.Empty;

            if (field.Length < startPosition) 
                return String.Empty;

            return field[startPosition..].Trim();
        }

        private void InternalInitialParseEmail(StringBuilder rawMail)
        {
            StringBuilder currentField = new(1000);

            for (int i = 0; i < rawMail.Length; i++)
            {
                bool canPeekAhead = i < rawMail.Length - 1;

                char c = rawMail[i];

                switch (c)
                {
                    case '\r':
                        continue;

                    case '\n':
                        if (canPeekAhead)
                        {
                            if (rawMail[i + 1] != ' ' && rawMail[i + 1] != '\t')
                            {
                                _fields.Add(currentField.ToString());
                                currentField.Clear();
                                continue;
                            }
                        }

                        _fields.Add(currentField.ToString());

                        break;

                    default:
                        currentField.Append(c);
                        break;

                }
            }

            if (currentField.Length > 0)
            {
                _fields.Add(currentField.ToString());
            }
        }
    }
}

#endif