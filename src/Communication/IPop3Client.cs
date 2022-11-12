/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2022 Simon Carter
 *
 *  Purpose:  Pop 3 client Class
 *
 */
using System;

#if NET5

namespace Shared.Communication
{
    public interface IPop3Client : IDisposable
    {
        void Initialize(string uri, string userName, string password, ushort port);

        bool IsConnected { get; }

        int GetMailCount(out int sizeInOctets);

        string RetrieveMessage(int messageNumber, out string readResponse);

        string DeleteMessage(int messageNumber);
    }
}

#endif