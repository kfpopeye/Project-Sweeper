using System.Collections.Generic;
using System;
using System.Windows;
using Autodesk.Revit.DB;

namespace PKHL.ProjectSweeper.FillPatternCleaner
{
    /// <summary>
    /// Description of FillPatternDefinition.
    /// </summary>
    public class FillPatternDefinition : BaseStyleDefinition
    {
        /// <summary>
        /// Drafting or model
        /// </summary>
        public FillPatternTarget TheTarget;
        public FillPattern thePattern { get; set; }
        public int MaterialUses { get; set; }
        public int ComponentUses { get; set; }
        public int RegionUses { get; set; }
        private List<AssetDefinition> OwnerAssets = null;
        private Units projectUnits = null;
        private enum pattType { Complex, Simple_Crosshatch, Simple_Parallel_Lines };
        private pattType _pattType = pattType.Simple_Parallel_Lines;
        private string _PatternType = null;

        /// <summary>
        /// Returns basedefinition.DeleteItems, used for clarity in code.
        /// </summary>
        public bool SetToNone
        {
            get
            {
                return base.DeleteElements;
            }
        }

        private int _familyUses { get; set; }
        public int FamilyUses { get { return _familyUses; } }

        /// <summary>
        /// Return localized version of Model or Drafting
        /// </summary>
        public string ItsType
        {
            get
            {
                return LocalizationProvider.GetLocalizedValue<string>(TheTarget.ToString());
            }
        }

        /// <summary>
        /// Complex, Simple Crosshatch or Simple Parallel Lines
        /// </summary>
        public string PatternType
        {
            get
            {
                if (thePattern == null)
                    return null;
                if (_PatternType == null)
                _PatternType = LocalizationProvider.GetLocalizedValue<string>(_pattType.ToString());
                return _PatternType;
            }
        }

        public string LineAngle
        {
            get
            {
                if (_pattType == pattType.Complex)
                    return null;
                if (thePattern == null)
                    return null;

                return UnitFormatUtils.Format(projectUnits, UnitType.UT_Angle, thePattern.GetFillGrid(0).Angle, false, false);
            }
        }

        public string LineSpacing1
        {
            get
            {
                if (_pattType == pattType.Complex || projectUnits == null)
                    return null;
                if (thePattern == null)
                    return null;

                return UnitFormatUtils.Format(projectUnits, UnitType.UT_Length, thePattern.GetFillGrid(0).Offset, false, false);
            }
        }

        public string LineSpacing2
        {
            get
            {
                if (_pattType == pattType.Complex || _pattType == pattType.Simple_Parallel_Lines || projectUnits == null)
                    return null;
                if (thePattern == null)
                    return null;

                return UnitFormatUtils.Format(projectUnits, UnitType.UT_Length, thePattern.GetFillGrid(1).Offset, false, false);
            }
        }

        /// <summary>
        /// Returns false if a family is using this pattern.
        /// </summary>
        public override bool IsDeleteable
        {
            get
            {
                if (_familyUses != 0)
                    return false;
                else
                    return base.IsDeleteable;
            }
            set
            {
                base.IsDeleteable = value;
            }
        }

        private bool itsDeletedStatus = false;
        /// <summary>
        /// Throws exception if setting to true and IsDeleteable is false
        /// </summary>
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
                    if(value == true)
                        throw new InvalidOperationException("Cannot delete the style: " + this.StyleName);
            }
        }

        public void AddAsset(AssetDefinition ad)
        {
            if (ad.Type == "Family")
            {
                if (!OwnerAssets.Contains(ad))
                {
                    OwnerAssets.Add(ad);
                    _familyUses++;
                }
            }
            else
                throw new ArgumentException("AddAsset received an asset that was not a family type");
        }

        public List<AssetDefinition> getAssets()
        {
            return OwnerAssets;
        }

        public override int NumberOfUses
        {
            get
            {
                return (MaterialUses + ComponentUses + RegionUses + FamilyUses);
            }
        }

        public FillPatternDefinition()
        {
            System.Diagnostics.Debug.WriteLine("FPD ctor : blank");
        }

        /// <summary>
        /// Does not include owner assets, used for handling user selected elements only
        /// </summary>
        public FillPatternDefinition(FillPatternElement fpe) : base()
        {
            StyleName = fpe.Name;
            System.Diagnostics.Debug.WriteLine("FPD ctor : " + StyleName);
            ItsId = fpe.Id.IntegerValue;
            thePattern = fpe.GetFillPattern();
            projectUnits = fpe.Document.GetUnits();

            if (thePattern != null)
            {
                TheTarget = thePattern.Target;
                if (thePattern.GridCount > 2)
                    _pattType = pattType.Complex;
                else if (thePattern.GridCount == 2)
                    _pattType = pattType.Simple_Crosshatch;
                else
                    _pattType = pattType.Simple_Parallel_Lines;
            }

            MaterialUses = 0;
            ComponentUses = 0;
            RegionUses = 0;
            _familyUses = 0;
        }

        /// <summary>
        /// DOES NOT SCAN FAMILIES
        /// </summary>
        /// <param name="fpe"></param>
        /// <param name="ComponentLib"></param>
        /// <param name="MaterialLib"></param>
        /// <param name="RegionLib"></param>
        public FillPatternDefinition(FillPatternElement fpe, ref IList<Element> ComponentLib, ref IList<Element> MaterialLib, ref IList<Element> RegionLib)
            : base()
        {
            StyleName = fpe.Name;
            System.Diagnostics.Debug.WriteLine("FPD ctor : " + StyleName);
            ItsId = fpe.Id.IntegerValue;
            thePattern = fpe.GetFillPattern();
            if (thePattern != null)
            {
                TheTarget = thePattern.Target;
                if (thePattern.GridCount > 2)
                    _pattType = pattType.Complex;
                else if (thePattern.GridCount == 2)
                    _pattType = pattType.Simple_Crosshatch;
                else
                    _pattType = pattType.Simple_Parallel_Lines;
            }
            OwnerAssets = new List<AssetDefinition>();
            projectUnits = fpe.Document.GetUnits();

            _familyUses = 0;

            MaterialUses = 0;
            foreach (Material m in MaterialLib)
            {
                if (m.CutForegroundPatternId == fpe.Id || 
                    m.CutBackgroundPatternId == fpe.Id ||
                    m.SurfaceForegroundPatternId == fpe.Id ||
                    m.SurfaceBackgroundPatternId == fpe.Id)
                {
                    MaterialUses++;
                    OwnerAssets.Add(new AssetDefinition(m));
                }
            }
            ComponentUses = 0;
            foreach (Element el in ComponentLib)
            {
                if (el.get_Parameter(BuiltInParameter.COARSE_SCALE_FILL_PATTERN_ID_PARAM).AsElementId() == fpe.Id)
                {
                    ComponentUses++;
                    OwnerAssets.Add(new AssetDefinition(el));
                }
            }
            RegionUses = 0;
            foreach (FilledRegionType frt in RegionLib)
            {
                Parameter p = frt.get_Parameter(BuiltInParameter.FOREGROUND_ANY_PATTERN_ID_PARAM);
                if (p == null)
                {
                    p = frt.get_Parameter(BuiltInParameter.FILL_PATTERN_ID_PARAM_NO_NO);
                }
                if (p != null && p.AsElementId() == fpe.Id)
                {
                    RegionUses++;
                    OwnerAssets.Add(new AssetDefinition(frt));
                }
                else
                {
                    p = frt.get_Parameter(BuiltInParameter.BACKGROUND_DRAFT_PATTERN_ID_PARAM);
                    if (p != null && p.AsElementId() == fpe.Id)
                    {
                        RegionUses++;
                        OwnerAssets.Add(new AssetDefinition(frt));
                    }
                }
            }
        }

        /// <summary>
        /// Compares ID and style name only
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            FillPatternDefinition rhs = obj as FillPatternDefinition;
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
