using System.Collections.Generic;
using System;
using Autodesk.Revit.DB;

namespace PKHL.ProjectSweeper.LinePatternCleaner
{
    /// <summary>
    /// Description of FillPatternDefinition.
    /// </summary>
    public class LinePatternDefinition : BaseStyleDefinition
    {
        public LinePattern thePattern { get; set; }
        public List<AssetDefinition> OwnerAssets = null;

        /// <summary>
        /// Overrides base definition, returns true if new style is not null, false otherwise.
        /// </summary>
        public override bool StyleToBeConverted
        {
            get
            {
                if (_newstyle != null)
                    return true;
                else
                    return false;
            }
        }

        private bool itsDeletedStatus = false;
        public override bool StyleToBeDeleted
        {
            get { return itsDeletedStatus; }
            set
            {
                if (IsDeleteable)
                {
                    itsDeletedStatus = value;
                    System.Diagnostics.Debug.WriteLine(StyleName + " : PatternToBeDeleted set to -> " + itsDeletedStatus.ToString());
                }
                else
                    throw new InvalidOperationException("Cannot delete the pattern: " + this.StyleName);
            }
        }

        public override int NumberOfUses
        {
            get
            {
                if (OwnerAssets == null)
                    return -1;
                else
                    return OwnerAssets.Count;
            }
        }

        public LinePatternDefinition()
        {
            System.Diagnostics.Debug.WriteLine("LPD ctor : blank");
        }

        /// <summary>
        /// This is used to create the user selction on single and multi mode. It does not include owner assets.
        /// </summary>
        /// <param name="_gs"></param>
        public LinePatternDefinition(GraphicsStyle _gs) : base()
        {
            ElementId eid = _gs.GraphicsStyleCategory.GetLinePatternId(_gs.GraphicsStyleType);
            LinePatternElement lpe = _gs.Document.GetElement(eid) as LinePatternElement;

            StyleName = lpe.Name;
            System.Diagnostics.Debug.WriteLine("LPD ctor (gs) : " + StyleName);
            ItsId = lpe.Id.IntegerValue;
            thePattern = lpe.GetLinePattern();
        }

        /// <summary>
        /// Used by Line Style Cleaner
        /// </summary>
        /// <param name="lpe"></param>
        public LinePatternDefinition(LinePatternElement lpe) : base()
        {
            StyleName = lpe.Name;
            System.Diagnostics.Debug.WriteLine("LPD ctor : " + StyleName);
            ItsId = lpe.Id.IntegerValue;
            thePattern = lpe.GetLinePattern();
        }

        public LinePatternDefinition(LinePatternElement lpe, ref IList<Element> allgs) : base()
        {
            StyleName = lpe.Name;
            System.Diagnostics.Debug.WriteLine("LPD ctor : " + StyleName);
            ItsId = lpe.Id.IntegerValue;
            thePattern = lpe.GetLinePattern();
            OwnerAssets = new List<AssetDefinition>();

            foreach (Element el2 in allgs)
            {
                GraphicsStyle gs = el2 as GraphicsStyle;
                if (gs.GraphicsStyleType != GraphicsStyleType.Cut && gs.GraphicsStyleType != GraphicsStyleType.Projection)
                    System.Diagnostics.Debug.WriteLine(gs.Name + ": gs styletype = " + gs.GraphicsStyleType.ToString());
                else
                {
                    ElementId eid = gs.GraphicsStyleCategory.GetLinePatternId(gs.GraphicsStyleType);
                    if (eid != LinePatternElement.GetSolidPatternId() && eid != ElementId.InvalidElementId)
                    {
                        LinePatternElement l = gs.Document.GetElement(eid) as LinePatternElement;
                        if (l != null)
                        {
                            LinePattern gs_lp = l.GetLinePattern();
                            if (gs_lp.Name == thePattern.Name)
                            {
                                AssetDefinition ad = new AssetDefinition(gs);
                                if (!OwnerAssets.Contains(ad))
                                {
                                    OwnerAssets.Add(ad);
                                }
                            }
                        }
                    }
                }
            }
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            LinePatternDefinition rhs = obj as LinePatternDefinition;
            if (this.StyleName != rhs.StyleName ||
                this.ItsId != rhs.ItsId)
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
