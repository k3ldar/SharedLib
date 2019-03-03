/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2012 Simon Carter
 *
 *  Purpose:  internally used WCF service contract
 *
 */
using System.ServiceModel;

namespace Shared
{
#if NET461
    /// <summary>
    /// Service contract
    /// </summary>
    [ServiceContract]
    public interface IWebDefenderStatus
    {
        /// <summary>
        /// Method for testing the connection
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        bool TestConnection();

        /// <summary>
        /// Get's current memory status
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        string GetMemoryStatus();

        /// <summary>
        /// Get's current memory status for Firebird
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        string GetMemoryStatusFirebird();

        /// <summary>
        /// Indicates that settings should be updated
        /// </summary>
        [OperationContract]
        void UpdateSettings();

        /// <summary>
        /// Indicates that Firebird settings should be updated
        /// </summary>
        [OperationContract]
        void UpdateSettingsFirebird();

        /// <summary>
        /// Indicates that WebState settings should be updated
        /// </summary>
        [OperationContract]
        void UpdateSettingsWebState();

        /// <summary>
        /// Indicates that domain settings should be updated
        /// </summary>
        [OperationContract]
        void UpdateSettingsDomain();
    }
#endif
}
