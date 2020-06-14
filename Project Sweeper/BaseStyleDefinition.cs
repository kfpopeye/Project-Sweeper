using System;
using System.ComponentModel;
using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace PKHL.ProjectSweeper
{
    public abstract class BaseStyleDefinition : INotifyPropertyChanged
    {
        public string StyleName { get; set; }
        public int ItsId { get; set; }
        public abstract int NumberOfUses { get; }

        /// <summary>
        /// if NewStyle id = -1
        /// </summary>
        public bool DeleteElements
        {
            get
            {
                if (_newstyle != null && _newstyle.ItsId == -1)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// If this.NewStyle is null or its Id = -1 false, otherwise true;
        /// </summary>
        public virtual bool StyleToBeConverted
        {
            get
            {
                if (_newstyle == null || _newstyle.ItsId == -1)
                    return false;
                else
                    return true;
            }
        }

        protected bool is_deleteable = true;
        public virtual bool IsDeleteable
        {
            get
            {
                return is_deleteable;
            }
            set
            {
                is_deleteable = value;
                OnPropertyChanged("IsDeleteable");
                if (is_deleteable)
                    System.Diagnostics.Debug.WriteLine(StyleName + " is now deletable.");
                else
                    System.Diagnostics.Debug.WriteLine(StyleName + " is no longer deletable.");
            }
        }

        public abstract bool StyleToBeDeleted { get; set; }

        protected Color _itsColour = null;
        public string StyleColour
        {
            get
            {
                //BUGFIX: check if valid
                if (_itsColour == null || !_itsColour.IsValid)
                    return string.Empty;
                else
                    return "#" + _itsColour.Red.ToString("X2") + _itsColour.Green.ToString("X2") + _itsColour.Blue.ToString("X2");
            }
        }

        protected BaseStyleDefinition _newstyle = null;
        /// <summary>
        /// If new style equals THIS, sets to null
        /// Returns THIS if new style is null, returns new style otherwise
        /// </summary>
        public BaseStyleDefinition NewStyle
        {
            get
            {
                    return _newstyle;
            }
            set
            {
                if (_newstyle != null && _newstyle.Equals(value))
                {
                    System.Diagnostics.Debug.WriteLine(StyleName + " newStyle = value. Did nothing.");
                    return;
                }
                if (this.Equals(value) || value == null)
                {
                    _newstyle = null;
                    System.Diagnostics.Debug.WriteLine(StyleName + " newStyle set to -> null");
                }
                else
                {
                    _newstyle = value;
                    System.Diagnostics.Debug.WriteLine(StyleName + " newStyle set to -> " + _newstyle.StyleName);
                }
                OnPropertyChanged("NewStyle");
                OnPropertyChanged("StyleToBeConverted");
            }
        }

        public BaseStyleDefinition() { }

        protected Color ConvertInt(int rgb)
        {
            byte[] bArray = BitConverter.GetBytes(rgb);
            if (BitConverter.IsLittleEndian)
                return new Color(bArray[0], bArray[1], bArray[2]);
            else
                return new Color(bArray[1], bArray[2], bArray[3]);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            BaseStyleDefinition rsd = obj as BaseStyleDefinition;
            if (this.StyleName != rsd.StyleName ||
                this.ItsId != rsd.ItsId)
                return false;
            return true;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return ItsId;
        }
    }
}
