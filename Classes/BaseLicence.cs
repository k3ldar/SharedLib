using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SieraDelta.Shared.Classes
{
    public abstract class BaseLicence
    {
        #region Public Methods

        /// <summary>
        /// Determines wether the licence is valid or not
        /// </summary>
        /// <param name="type">Type of licence</param>
        /// <returns>True if valid, otherwise false</returns>
        public abstract bool LicenceValid(LicenceType type);

        /// <summary>
        /// Forces the licence to validate itself
        /// </summary>
        /// <returns>true if licence valid, otherwise false</returns>
        public abstract bool ValidateLicence(ref bool errorOnValidate, bool save);


        /// <summary>
        /// Loads a licence from a file
        /// </summary>
        /// <param name="fileName">Licence File</param>
        /// <returns>WebDefenderLicence object with licence settings</returns>
        public abstract BaseLicence Load(LicenceType type);

        /// <summary>
        /// Save's encrypted licence to file
        /// </summary>
        /// <param name="type">type of licence</param>
        /// <param name="licence">encrypted licence</param>
        public abstract void Save(LicenceType type, string licence);

        /// <summary>
        /// Request Free Trial Licence
        /// </summary>
        /// <param name="type">Type of licence being requested</param>
        public abstract string RequestTrial(LicenceType type);

        #endregion Public Methods
    }
}
