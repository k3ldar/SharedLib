/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  Global Enums
 *
 */
using System;
using System.Text;

namespace Shared
{
    /// <summary>
    /// 
    /// </summary>
    public enum ToastEventType
    {
        /// <summary>
        /// No Event
        /// </summary>
        None, 

        /// <summary>
        /// timeout
        /// </summary>
        Timeout,

        /// <summary>
        /// Clicked
        /// </summary>
        Clicked,

        /// <summary>
        /// focused
        /// </summary>
        Focused,

        /// <summary>
        /// Cancelled
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// Type of Column
    /// </summary>
    public enum ColumnType
    {
        /// <summary>
        /// Decimal Column Type
        /// </summary>
        Decimal,

        /// <summary>
        /// String Column Type
        /// </summary>
        String,

        /// <summary>
        /// Integer Column Type
        /// </summary>
        Integer,

        /// <summary>
        /// Long column type
        /// </summary>
        Int64,

        /// <summary>
        /// Bool Column Type
        /// </summary>
        Boolean
    }

    /// <summary>
    /// Position of notification Form
    /// </summary>
    public enum NotificationPosition
    {
        /// <summary>
        /// Top Left of active screen
        /// </summary>
        TopLeft,

        /// <summary>
        /// Top right of active screen
        /// </summary>
        TopRight,

        /// <summary>
        /// Bottom Left of active screen
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Bottom Right of active screen
        /// </summary>
        BottomRight
    }

    /// <summary>
    /// Type of notification window
    /// </summary>
    public enum NotificationEffect
    {
        /// <summary>
        /// Notification just appears
        /// </summary>
        None,

        /// <summary>
        /// Notification Window fades In and remains
        /// </summary>
        FadeIn,

        /// <summary>
        /// Notification window appears and fades out
        /// </summary>
        FadeOut,

        /// <summary>
        /// Notification window fades in and fades out after xx seconds
        /// </summary>
        FadeInOut,

        /// <summary>
        /// Notification windows slides in to view
        /// </summary>
        Slide
    }

    /// <summary>
    /// Database Connection Type
    /// 
    /// if used in int conversion, then the value is default port for server
    /// </summary>
    public enum DatabaseConnectionType
    {
        /// <summary>
        /// Firebird
        /// </summary>
        Firebird = 3050,

        /// <summary>
        /// MySQL 
        /// </summary>
        MySQL = 3306,

        /// <summary>
        /// Microsoft SQL Server
        /// </summary>
        MSSQL = 0
    }

    /// <summary>
    /// Type of Validation
    /// </summary>
    public enum ValidationTypes 
    { 
        /// <summary>
        /// Credit card validation check
        /// </summary>
        CreditCard,
 
        /// <summary>
        /// Is numeric input validation check
        /// </summary>
        IsNumeric, 

        /// <summary>
        /// Alpha numeric validation check
        /// </summary>
        AlphaNumeric, 

        /// <summary>
        /// A to Z validation check
        /// </summary>
        AtoZ, 

        /// <summary>
        /// Name validation check
        /// </summary>
        Name, 

        /// <summary>
        /// Credit card valid from validation check
        /// </summary>
        CardValidFrom, 

        /// <summary>
        /// Credit card valid to validation check
        /// </summary>
        CardValidTo, 

        /// <summary>
        /// File name validation check
        /// </summary>
        FileName 
    }

    /// <summary>
    /// Credit Card Types
    /// </summary>
    public enum AcceptedCreditCardTypes 
    { 
        /// <summary>
        /// Visa card
        /// </summary>
        Visa = 1, 

        /// <summary>
        /// Master card
        /// </summary>
        MasterCard = 2, 

        /// <summary>
        /// Visa Debit card
        /// </summary>
        VisaDebit = 4,

        /// <summary>
        /// American expres card
        /// </summary>
        AmericanExpress = 8,

        /// <summary>
        /// Diners club card
        /// </summary>
        DinersClub = 16,

        /// <summary>
        /// JCB card
        /// </summary>
        JCB = 32,

        /// <summary>
        /// Visa Master card
        /// </summary>
        VisaMaster = 64,

        /// <summary>
        /// Maestro card
        /// </summary>
        Maestro = 128,

        /// <summary>
        /// Solo card
        /// </summary>
        Solo = 256,

        /// <summary>
        /// Discover card
        /// </summary>
        Discover = 512,

        /// <summary>
        /// Switch card
        /// </summary>
        Switch = 1024,

        /// <summary>
        /// Carte blance card
        /// </summary>
        CarteBlanch = 2048,

        /// <summary>
        /// Insta payment card
        /// </summary>
        InstaPayment = 4096,

        /// <summary>
        /// Laser card
        /// </summary>
        Laser = 8192,

        /// <summary>
        /// Union pay card
        /// </summary>
        UnionPay = 16384,

        /// <summary>
        /// Korean local card
        /// </summary>
        KoreanLocal = 32768,

        /// <summary>
        /// BC Global card
        /// </summary>
        BCGlobal = 65536
    }

    /// <summary>
    /// Case Type
    /// </summary>
    public enum CaseType 
    { 
        /// <summary>
        /// Ignore case type
        /// </summary>
        Ignore, 
        
        /// <summary>
        /// Upper case
        /// </summary>
        Upper, 
        
        /// <summary>
        /// Lower case
        /// </summary>
        Lower, 
        
        /// <summary>
        /// Proper case
        /// </summary>
        Proper 
    }

    /// <summary>
    /// Validate Request Results
    /// </summary>
    [Flags]
    public enum ValidateRequestResult
    {
        /// <summary>
        /// Initialise has not been called
        /// </summary>
        NotInitialised = 1,

        /// <summary>
        /// State unknown
        /// </summary>
        Undetermined = 2,

        /// <summary>
        /// Enough keywords to suggest may be a SQL injection attack
        /// </summary>
        PossibleSQLInjectionAttack = 4,

        /// <summary>
        /// Enough keywords to determine this is a SQL injection attack
        /// </summary>
        SQLInjectionAttack = 8,

        /// <summary>
        /// Determines that the request is probably generated from a spider or bot
        /// </summary>
        PossibleSpiderBot = 16,

        /// <summary>
        /// Determines that the request is generated from a spider or bot
        /// </summary>
        SpiderBot = 32,

        /// <summary>
        /// Enough keywords to suggest this maybe a hack attempt
        /// </summary>
        PossibleHackAttempt = 64,

        /// <summary>
        /// Enough keywords to determine this is a hack attempt
        /// </summary>
        HackAttempt = 128,

        /// <summary>
        /// IP Address is white listed
        /// </summary>
        IPWhiteListed = 256,

        /// <summary>
        /// IP Address is black listed
        /// </summary>
        IPBlackListed = 512,

        /// <summary>
        /// IP address is a search engine
        /// </summary>
        SearchEngine = 1024,

        /// <summary>
        /// Indicates the product licence is invalid
        /// </summary>
        InvalidLicence = 2048,

        /// <summary>
        /// A Ban has been requested on the IP Address
        /// </summary>
        BanRequested = 4096,

        /// <summary>
        /// Connection is a port scanner for single port
        /// </summary>
        SinglePortScanner = 8192,

        /// <summary>
        /// Connection is a multiple port scanner
        /// </summary>
        MultiPortScanner = 16384
    }

    /// <summary>
    /// Address Type
    /// </summary>
    public enum AddressType
    {
        /// <summary>
        /// Fixed address type, created by system
        /// </summary>
        Fixed = 0,

        /// <summary>
        /// Address type is standard 
        /// </summary>
        HackerOrSQL = 1,

        /// <summary>
        /// Address type is a failed audit
        /// </summary>
        FailedAudit = 2,

        /// <summary>
        /// User Defined address type
        /// </summary>
        UserDefined = 3,

        /// <summary>
        /// Firebird generated Audit
        /// </summary>
        FirebirdAudit = 4,

        /// <summary>
        /// MailEnable Failed Audit
        /// </summary>
        MailEnableAudit = 5
    }

    /// <summary>
    /// Type of licence
    /// </summary>
    public enum LicenceType
    {
        /// <summary>
        /// Licence is for a website domain, including subdomains
        /// </summary>
        Domain = 0,

        /// <summary>
        /// Licence is for a server
        /// </summary>
        Server = 1,

        /// <summary>
        /// Licence is for Firebird
        /// </summary>
        Firebird = 2,

        /// <summary>
        /// Licence is for WebMonitor
        /// </summary>
        WebMonitor = 3,

        /// <summary>
        /// License is for GeoIP
        /// </summary>
        GeoIP = 4,

        /// <summary>
        /// Licence is for Service Guard
        /// </summary>
        ServiceGuard = 5,

        /// <summary>
        /// Firebird Task Scheduler
        /// </summary>
        FBTaskScheduler = 6,

        /// <summary>
        /// Replication Engine
        /// </summary>
        ReplicationEngine = 7
    }
}
