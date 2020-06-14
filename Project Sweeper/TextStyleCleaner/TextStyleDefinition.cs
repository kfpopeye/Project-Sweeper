using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace PKHL.ProjectSweeper.TextStyleCleaner
{
    public class TextStyleDefinition : ViewOwnerDefinition
    {
        //Graphic settings
        public string GraphicColour
        {
            get
            {
                if (_itsColour == null)
                    return null;
                return "#" + _itsColour.Red.ToString("X2") + _itsColour.Green.ToString("X2") + _itsColour.Blue.ToString("X2");
            }
        }
        public int GraphicWeight { get; set; }
        public string GraphicBackground { get; set; }
        public string GraphicShowBorder { get; set; }
        public string GraphicLeaderBorderOffset { get; set; }
        public string GraphicLeaderArrowhead { get; set; }

        //Text settings
        public string TextFontName { get; set; }
        public string TextSize { get; set; }
        public string TextTabSize { get; set; }
        public int TextBold { get; set; }
        public int TextItalic { get; set; }
        public int TextUnderline { get; set; }
        public string TextWidthFactor { get; set; }


        private bool _deleteMe = false;
        public override bool StyleToBeDeleted
        {
            get
            {
                return _deleteMe;
            }
            set
            {
                _deleteMe = value;
                System.Diagnostics.Debug.WriteLine(StyleName + ": StyleToBeDeleted set to -> " + _deleteMe.ToString());
                OnPropertyChanged("StyleToBeDeleted");
            }
        }

        public override int NumberOfUses
        {
            get
            {
                int x = 0, y = 0;
                if (OwnerViews != null)
                    x = OwnerViews.Count;
                if (OwnerSchedules != null)
                    y = OwnerSchedules.Count;
                return x + y;
            }
        }

        public TextStyleDefinition() : base()
        {
            System.Diagnostics.Debug.WriteLine("TSD ctor : blank");
        }

        public TextStyleDefinition(TextNoteType tnt) : base()
        {
            StyleName = tnt.Name;
            System.Diagnostics.Debug.WriteLine("TSD ctor : " + StyleName);
            ItsId = tnt.Id.IntegerValue;

            _itsColour = ConvertInt(tnt.get_Parameter(BuiltInParameter.LINE_COLOR).AsInteger());
            GraphicWeight = tnt.get_Parameter(BuiltInParameter.LINE_PEN).AsInteger();
            GraphicBackground = tnt.get_Parameter(BuiltInParameter.TEXT_BACKGROUND).AsValueString();
            GraphicShowBorder = tnt.get_Parameter(BuiltInParameter.TEXT_BOX_VISIBILITY).AsValueString();
            GraphicLeaderBorderOffset = tnt.get_Parameter(BuiltInParameter.LEADER_OFFSET_SHEET).AsValueString();
            Element el = tnt.Document.GetElement(tnt.get_Parameter(BuiltInParameter.LEADER_ARROWHEAD).AsElementId());
            GraphicLeaderArrowhead = (el == null) ? "None" : el.Name;

            TextFontName = tnt.get_Parameter(BuiltInParameter.TEXT_FONT).AsString();
            TextSize = tnt.get_Parameter(BuiltInParameter.TEXT_SIZE).AsValueString();
            TextTabSize = tnt.get_Parameter(BuiltInParameter.TEXT_TAB_SIZE).AsValueString();
            TextBold = tnt.get_Parameter(BuiltInParameter.TEXT_STYLE_BOLD).AsInteger();
            TextItalic = tnt.get_Parameter(BuiltInParameter.TEXT_STYLE_ITALIC).AsInteger();
            TextUnderline = tnt.get_Parameter(BuiltInParameter.TEXT_STYLE_UNDERLINE).AsInteger();
            TextWidthFactor = tnt.get_Parameter(BuiltInParameter.TEXT_WIDTH_SCALE).AsValueString();
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            TextStyleDefinition lsd = obj as TextStyleDefinition;
            if (this.StyleName != lsd.StyleName ||
                this.ItsId != lsd.ItsId)
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
