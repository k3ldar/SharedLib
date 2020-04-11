/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2012 Simon Carter
 *
 *  Purpose:  Credit Card Validation
 *
 */
using System;
using System.Text.RegularExpressions;

namespace Shared
{
    /// <summary>
    /// Performs basic validation
    /// </summary>
    public static class Validation
    {
        private const string ALLOWED_CHARS_NAME = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-' ";
        private const string ALLOWED_CHARS_ALPHANUMERIC = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string ALLOWED_CHARS_FILENAME = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-.";
        private const string ALLOWED_CHARS_A_TO_Z = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string ALLOWED_CHARS_NUMBER = "0123456789";
        private const string ALLOWED_CHARS_CARD_DATE = "0123456789/";

        private const string CARD_TYPE_MASTERCARD = "^5[1-5][0-9]{14}$";
        private const string CARD_TYPE_VISA = "^4[0-9]{12}(?:[0-9]{3})?$";
        private const string CARD_TYPE_AMEX = "^3[47][0-9]{13}$";
        private const string CARD_TYPE_CARTE_BLANCH = "^389[0-9]{11}$";
        private const string CARD_TYPE_DINERS_CLUB = "^3(?:0[0-5]|[68][0-9])[0-9]{11}$";
        private const string CARD_TYPE_DISCOVER = "^65[4-9][0-9]{13}|64[4-9][0-9]{13}|6011[0-9]{12}|(622(?:12[6-9]|1[3-9][0-9]|[2-8][0-9][0-9]|9[01][0-9]|92[0-5])[0-9]{10})$";
        private const string CARD_TYPE_JCB = @"^(?:2131|1800|35\d{3})\d{11}$";
        private const string CARD_TYPE_VISA_MASTER = "^(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14})$";
        private const string CARD_TYPE_INSTA_PAYMENT = "^63[7-9][0-9]{13}$";
        private const string CARD_TYPE_LASER = "^(6304|6706|6709|6771)[0-9]{12,15}$";
        private const string CARD_TYPE_MAESTRO = "^(5018|5020|5038|6304|6759|6761|6763)[0-9]{8,15}$";
        private const string CARD_TYPE_SOLO = "^(6334|6767)[0-9]{12}|(6334|6767)[0-9]{14}|(6334|6767)[0-9]{15}$";
        private const string CARD_TYPE_SWITCH = "^(4903|4905|4911|4936|6333|6759)[0-9]{12}|(4903|4905|4911|4936|6333|6759)[0-9]{14}|(4903|4905|4911|4936|6333|6759)[0-9]{15}|564182[0-9]{10}|564182[0-9]{12}|564182[0-9]{13}|633110[0-9]{10}|633110[0-9]{12}|633110[0-9]{13}$";
        private const string CARD_TYPE_UNION_PAY = "^(62[0-9]{14,17})$";
        private const string CARD_TYPE_KOREAN_LOCAL = "^9[0-9]{15}$";
        private const string CARD_TYPE_BC_GLOBAL = "^(6541|6556)[0-9]{12}$";

        #region Public Static Methods

        /// <summary>
        /// Validates a string against minimum / maximum length
        /// 
        /// ArgumentException thrown if validationText does not meet requirements
        /// </summary>
        /// <param name="validationText">string to be validated</param>
        /// <param name="minimumLength">Minimum Length, zero means no minimum length</param>
        /// <param name="maximumLength">Maximum Length, zero means no maximum length</param>
        /// <param name="fieldName">Name of field </param>
        public static void Validate(string validationText, int minimumLength, int maximumLength, string fieldName)
        {
            if (minimumLength > 0 && validationText.Trim().Length < minimumLength)
                throw new ArgumentException(String.Format("Minimum length for {0} is {1}", fieldName, minimumLength));

            if (maximumLength > 0 && validationText.Trim().Length > maximumLength)
                throw new ArgumentException(String.Format("Maximum length for {0} is {1}", fieldName, maximumLength));
        }

        /// <summary>
        /// Validates string against ValidationType
        /// 
        /// If the text does not match the ValidationType then an error is raised
        /// </summary>
        /// <param name="validationText">string to validate</param>
        /// <param name="validationType">type of validation</param>
        /// <returns>Validated string</returns>
        public static string Validate(string validationText, ValidationTypes validationType)
        {
            string Result = validationText;

            switch (validationType)
            {
                case ValidationTypes.AlphaNumeric:
                    Result = RemoveInvalidChars(Result, ALLOWED_CHARS_ALPHANUMERIC);

                    break;
                case ValidationTypes.AtoZ:
                    Result = RemoveInvalidChars(Result, ALLOWED_CHARS_A_TO_Z);
                    break;

                case ValidationTypes.CreditCard:
                    Result = RemoveInvalidChars(Result, ALLOWED_CHARS_NUMBER);
                    CardType(Result);

                    break;
                case ValidationTypes.IsNumeric:
                    Result = RemoveInvalidChars(Result, ALLOWED_CHARS_NUMBER);

                    break;
                case ValidationTypes.Name:
                    Result = RemoveInvalidChars(Result, ALLOWED_CHARS_NAME);

                    break;
                case ValidationTypes.CardValidFrom:
                    Result = RemoveInvalidChars(Result, ALLOWED_CHARS_CARD_DATE);
                    CardDateValid(Result, false);

                    break;

                case ValidationTypes.CardValidTo:
                    Result = RemoveInvalidChars(Result, ALLOWED_CHARS_CARD_DATE);
                    CardDateValid(Result, true);
                    break;

                case ValidationTypes.FileName:
                    Result = RemoveInvalidChars(Result, ALLOWED_CHARS_FILENAME);

                    break;

                default:
                    throw new ArgumentException("Invalid ValidationType, or validationType not handled");
            }

            //assume if it's zero length then error
            if (String.IsNullOrEmpty(Result))
                throw new FormatException(String.Format("{0} is not of type {1}", validationText, validationType));

            return Result;
        }

        /// <summary>
        /// Given a card number, determines what type of credit/debit card it is
        /// </summary>
        /// <param name="cardNumber">Card number to check</param>
        /// <returns>AcceptedCreditCardTypes card type</returns>
        public static AcceptedCreditCardTypes CardType(string cardNumber)
        {
            if (Regex.IsMatch(cardNumber, CARD_TYPE_VISA))
                return AcceptedCreditCardTypes.Visa;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_MASTERCARD))
                return AcceptedCreditCardTypes.MasterCard;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_AMEX))
                    return AcceptedCreditCardTypes.AmericanExpress;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_CARTE_BLANCH))
                    return AcceptedCreditCardTypes.CarteBlanch;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_DINERS_CLUB))
                    return AcceptedCreditCardTypes.DinersClub;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_DISCOVER))
                    return AcceptedCreditCardTypes.Discover;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_JCB))
                    return AcceptedCreditCardTypes.JCB;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_VISA_MASTER))
                    return AcceptedCreditCardTypes.VisaMaster;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_INSTA_PAYMENT))
                    return AcceptedCreditCardTypes.InstaPayment;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_LASER))
                    return AcceptedCreditCardTypes.Laser;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_MAESTRO))
                    return AcceptedCreditCardTypes.Maestro;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_SOLO))
                    return AcceptedCreditCardTypes.Solo;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_SWITCH))
                    return AcceptedCreditCardTypes.Switch;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_UNION_PAY))
                    return AcceptedCreditCardTypes.UnionPay;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_KOREAN_LOCAL))
                    return AcceptedCreditCardTypes.KoreanLocal;

            if (Regex.IsMatch(cardNumber, CARD_TYPE_BC_GLOBAL))
                    return AcceptedCreditCardTypes.BCGlobal;

            throw new Exception("Could not determine credit card type");
        }



        #endregion Public Static Methods


        #region Private Static Methods

        private static void CardDateValid(string s, bool futureDate)
        {
            if (s.Length != 5)
                throw new Exception("Invalid Credit/Debit Valid Date");

            string[] parts = s.Split('/');

            DateTime cardDate = new DateTime(Convert.ToInt32("20" + parts[1]), Convert.ToInt32(parts[0]), 1);

            if (futureDate)
            {
                if (cardDate.Date < DateTime.Now.Date)
                    throw new Exception("Valid To Date can not be in the past.");
            }
            else
            {
                if (cardDate.Date > DateTime.Now.Date)
                    throw new Exception("Valid From Date can not be in the future.");
            }
        }

        private static string RemoveInvalidChars(string s, string validChars)
        {
            string Result = "";

            foreach (char c in s)
            {
                if (validChars.Contains(c.ToString()))
                    Result += c.ToString();
            }

            return Result;
        }

        //private static string ValidateNumeric(string validationText)
        //{
        //    if (String.IsNullOrEmpty(validationText))
        //        throw new Exception("Invalid Number");

        //    Convert.ToInt64(validationText);

        //    return (validationText);
        //}

        private static void ValidateCreditCard(string cardNumber)
        {
            int i, checkSum = 0;

            // Compute checksum of every other digit starting from right-most digit
            for (i = cardNumber.Length - 1; i >= 0; i -= 2)
                checkSum += cardNumber[i] - '0';

            // Now take digits not included in first checksum, multiple by two,
            // and compute checksum of resulting digits
            for (i = cardNumber.Length - 2; i >= 0; i -= 2)
            {
                int val = (cardNumber[i] - '0') * 2;
                while (val > 0)
                {
                    checkSum += val % 10;
                    val /= 10;
                }
            }

            // Number is valid if sum of both checksums MOD 10 equals 0
            if ((checkSum % 10) != 0)
                throw new Exception("Credit Card number does not appear to be valid");
        }

        #endregion Private Static Methods
    }
}
