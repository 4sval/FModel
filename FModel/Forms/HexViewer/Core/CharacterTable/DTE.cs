//////////////////////////////////////////////
// Apache 2.0  - 2003-2019
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;

namespace WpfHexaEditor.Core.CharacterTable
{
    /// <summary>
    /// Objet représentant un DTE.
    /// </summary>
    public sealed class Dte
    {
        /// <summary>Nom du DTE</summary>
        private string _entry;

        #region Constructeurs

        /// <summary>
        /// Constructeur principal
        /// </summary>
        public Dte()
        {
            _entry = string.Empty;
            Type = DteType.Invalid;
            Value = string.Empty;
        }

        /// <summary>
        /// Contructeur permetant d'ajouter une entrée et une valeur
        /// </summary>
        /// <param name="entry">Nom du DTE</param>
        /// <param name="value">Valeur du DTE</param>
        public Dte(string entry, string value)
        {
            _entry = entry;
            Value = value;
            Type = DteType.DualTitleEncoding;
        }

        /// <summary>
        /// Contructeur permetant d'ajouter une entrée, une valeur et une description
        /// </summary>
        /// <param name="entry">Nom du DTE</param>
        /// <param name="value">Valeur du DTE</param>
        /// <param name="type">Type de DTE</param>
        public Dte(string entry, string value, DteType type)
        {
            _entry = entry;
            Value = value;
            Type = type;
        }

        #endregion Constructeurs

        #region Propriétés

        /// <summary>
        /// Nom du DTE
        /// </summary>
        public string Entry
        {
            set => _entry = value != null ? value.ToUpper(): string.Empty;
            get => _entry;
        }

        /// <summary>
        /// Valeur du DTE
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Type de DTE
        /// </summary>
        public DteType Type { get; }

        #endregion Propriétés

        #region Méthodes

        /// <summary>
        /// Cette fonction permet de retourner le DTE sous forme : [Entry]=[Valeur]
        /// </summary>
        /// <returns>Retourne le DTE sous forme : [Entry]=[Valeur]</returns>
        public override string ToString() => Type != DteType.EndBlock && Type != DteType.EndLine
            ? _entry + "=" + Value
            : _entry;

        #endregion Méthodes

        #region Methodes Static

        public static DteType TypeDte(Dte dteValue)
        {
            if (dteValue == null) return DteType.Invalid;

            try
            {
                switch (dteValue._entry.Length)
                {
                    case 2:
                        return dteValue.Value.Length == 2 ? DteType.Ascii : DteType.DualTitleEncoding;
                    case 4: // >2
                        return DteType.MultipleTitleEncoding;
                }
            }
            catch (IndexOutOfRangeException)
            {
                switch (dteValue._entry)
                {
                    case @"/":
                        return DteType.EndBlock;

                    case @"*":
                        return DteType.EndLine;
                    //case @"\":
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                //Du a une entre qui a 2 = de suite... EX:  XX==
                return DteType.DualTitleEncoding;
            }

            return DteType.Invalid;
        }

        public static DteType TypeDte(string dteValue)
        {
            if (dteValue == null) return DteType.Invalid;

            try
            {
                if (dteValue == FModel.Properties.Resources.EndTagString)
                    return DteType.EndBlock; //<end>

                if (dteValue == FModel.Properties.Resources.LineTagString)
                    return DteType.EndLine; //<ln>

                switch (dteValue.Length)
                {
                    case 1:
                        return DteType.Ascii;
                    case 2:
                        return DteType.DualTitleEncoding;
                }

                if (dteValue.Length > 2)
                    return DteType.MultipleTitleEncoding;
            }
            catch (ArgumentOutOfRangeException)
            {
                //Du a une entre qui a 2 = de suite... EX:  XX==
                return DteType.DualTitleEncoding;
            }

            return DteType.Invalid;
        }

        #endregion Methodes Static

    }
}