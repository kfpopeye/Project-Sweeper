namespace PKHL.ProjectSweeper
{
    class Constants
    {
        public static string LSC_HELP = @"LineStyleCleaner.htm";
        public static string LPC_HELP = @"LinePatternCleaner.htm";
        public static string TSC_HELP = @"TextStyleCleaner.htm";
        public static string FRT_HELP = @"FillRegionTypeCleaner.htm";
        public static string FPC_HELP = @"FillPatternCleaner.htm";

#if MULTILICENSE
        public static string PRODUCTID = @"82cd28c4-2c4a-41d0-87cf-99cbf0faa369";
        public static string GROUP_NAME = "Project Sweeper ML";
#else        
        public static string GROUP_NAME = "Project Sweeper";
        public static string APP_STORE_ID = @"appstore.exchange.autodesk.com:projectsweeper";
#endif
    }
}
