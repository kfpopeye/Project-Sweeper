
using System;
using Autodesk.Revit.DB;
using System.ComponentModel;
using System.Collections.Generic;

namespace PKHL.ProjectSweeper.FillRegionTypeCleaner
{
    /// <summary>
    /// Description of FillPatternDefinition.
    /// </summary>
    public class FillRegionTypeDefinition : ViewOwnerDefinition
    {
		public string LineWeight { get; set; }
        public bool IsMasking { get; set; }
        public FillPattern ForegroundPattern { get; set; }
        public FillPattern BackgroundPattern { get; set; }
        public string BgPattType { get; set; }
        public string FgPattType { get; set; }
        private int uses = 0;
        private bool itsDeletedStatus = false;
        protected Color _itsBgColour = null;

        public string ForegroundFpColour
        {
            get
            {
                //BUGFIX: check if valid
                if (base._itsColour == null || !base._itsColour.IsValid)
                    return string.Empty;
                else
                    return "#" + base._itsColour.Red.ToString("X2") + base._itsColour.Green.ToString("X2") + base._itsColour.Blue.ToString("X2");
            }
        }

        public string BackgroundFpColour
        {
            get
            {
                //BUGFIX: check if valid
                if (_itsBgColour == null || !_itsBgColour.IsValid)
                    return string.Empty;
                else
                    return "#" + _itsBgColour.Red.ToString("X2") + _itsBgColour.Green.ToString("X2") + _itsBgColour.Blue.ToString("X2");
            }
        }

        public string ForePattName
        {
            get
            {
                if (ForegroundPattern == null)
                    return LocalizationProvider.GetLocalizedValue<string>("FRTC_NoFPName");
                else
                    return ForegroundPattern.Name;
            }
        }

        public string BackPattName
        {
            get
            {
                if (BackgroundPattern == null)
                    return LocalizationProvider.GetLocalizedValue<string>("FRTC_NoFPName");
                else
                    return BackgroundPattern.Name;
            }
        }

        public override bool StyleToBeDeleted
        {
            get { return itsDeletedStatus; }
            set
            {
                if (IsDeleteable)
                {
                    itsDeletedStatus = value;
                    System.Diagnostics.Debug.WriteLine(StyleName + " : StyleToBeDeleted set to -> " + itsDeletedStatus.ToString());
                }
                else
                    throw new InvalidOperationException("Cannot delete the line style: " + this.StyleName);
            }
        }

        public override int NumberOfUses
        {
            get
            {
                return uses;
            }
        }

        public void SetOwnerViews(IList<Element> RList)
        {
            if (RList.Count == 0)
                return;

            this.OwnerViews = new Dictionary<int, string>();
            foreach (Element rel in RList)
            {
                if (rel.OwnerViewId != ElementId.InvalidElementId)
                {
                    string view_name = rel.Document.GetElement(rel.OwnerViewId).Name;
                    if (!this.OwnerViews.ContainsKey(rel.OwnerViewId.IntegerValue))
                        this.OwnerViews.Add(rel.OwnerViewId.IntegerValue, view_name);
                }
            }

            uses = RList.Count;
        }

        public FillRegionTypeDefinition() : base()
		{
			System.Diagnostics.Debug.WriteLine("FRD ctor : blank");
		}
		
		public FillRegionTypeDefinition(FilledRegionType frt) : base()
		{
			StyleName = frt.Name;
			System.Diagnostics.Debug.WriteLine("FRD ctor : " + StyleName);
			ItsId = frt.Id.IntegerValue;
            _itsColour = frt.ForegroundPatternColor;
            _itsBgColour = frt.BackgroundPatternColor;
            this.IsMasking = frt.IsMasking;
			LineWeight = frt.get_Parameter(BuiltInParameter.LINE_PEN).AsValueString();

            //get foreground pattern
			Element el = frt.Document.GetElement(frt.ForegroundPatternId);
			if(el == null)
			{
				FgPattType = "N/A";
				System.Diagnostics.Debug.WriteLine("el=null: " + StyleName);
			}
			else
			{
				FillPatternElement fpe = el as FillPatternElement;
                ForegroundPattern = fpe.GetFillPattern();
                FgPattType = LocalizationProvider.GetLocalizedValue<string>(ForegroundPattern.Target.ToString()); //Model or Drafting
			}

            //get background pattern
            el = frt.Document.GetElement(frt.BackgroundPatternId);
            if (el == null)
            {
                BgPattType = "N/A";
                System.Diagnostics.Debug.WriteLine("el=null: " + StyleName);
            }
            else
            {
                FillPatternElement fpe = el as FillPatternElement;
                BackgroundPattern = fpe.GetFillPattern();
                BgPattType = LocalizationProvider.GetLocalizedValue<string>(BackgroundPattern.Target.ToString()); //Model or Drafting
            }
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            FillRegionTypeDefinition rhs = obj as FillRegionTypeDefinition;
            if (this.StyleName != rhs.StyleName ||
                this.ItsId != rhs.ItsId ||
                this.ForePattName != rhs.ForePattName ||
                this.BackPattName != rhs.BackPattName)
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
