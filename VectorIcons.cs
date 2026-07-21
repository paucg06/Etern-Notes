using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DevPlanner
{
    public static class VectorIcons
    {
        public const string Calendar = "M19,4H18V2H16V4H8V2H6V4H5C3.89,4 3,4.9 3,6V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V6A2,2 0 0,0 19,4M19,19H5V9H19V19M5,7V6H19V7H5Z";
        public const string Link = "M10.59,13.41C11.37,14.19 12.63,14.19 13.41,13.41L20,6.83C21.11,5.72 21.11,3.93 20,2.82C18.89,1.71 17.1,1.71 16,2.82L9.41,9.41C8.63,10.19 8.63,11.45 9.41,12.23L10.59,13.41M13.41,10.59L12.23,9.41C11.45,8.63 10.19,8.63 9.41,9.41L2.82,16C1.71,17.1 1.71,18.89 2.82,20C3.93,21.11 5.72,21.11 6.83,20L13.41,13.41C14.19,12.63 14.19,11.37 13.41,10.59Z";
        public const string User = "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z";
        public const string Trash = "M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z";
        public const string Edit = "M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.07,6.19L3,17.25Z";
        public const string Plus = "M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z";
        public const string Close = "M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z";
        public const string Minimize = "M20,14H4V12H20V14Z";
        public const string Maximize = "M4,4H20V20H4V4M6,6V18H18V6H6Z";
        public const string Restore = "M4,8H8V4H20V16H16V20H4V8M16,8V14H18V6H10V8H16M6,10V18H14V10H6Z";
        public const string Check = "M9,20.42L2.79,14.21L5.62,11.38L9,14.77L18.88,4.88L21.71,7.71L9,20.42Z";
        public const string Folder = "M20,18A2,2 0 0,1 18,20H6A2,2 0 0,1 4,18V6C4,4.89 4.9,4 6,4H12L14,6H18A2,2 0 0,1 20,8V18M20,8H6V18H20V8Z";
        public const string Gamepad = "M21,6H3A2,2 0 0,0 1,8V16A2,2 0 0,0 3,18H21A2,2 0 0,0 23,16V8A2,2 0 0,0 21,6M6,15H5V13H3V12H5V10H6V12H8V13H6V15M15.5,12A1.5,1.5 0 0,1 14,10.5A1.5,1.5 0 0,1 15.5,9A1.5,1.5 0 0,1 17,10.5A1.5,1.5 0 0,1 15.5,12M18.5,15A1.5,1.5 0 0,1 17,13.5A1.5,1.5 0 0,1 18.5,12A1.5,1.5 0 0,1 20,13.5A1.5,1.5 0 0,1 18.5,15Z";
        public const string Video = "M17,10.5V7A1,1 0 0,0 16,6H4A1,1 0 0,0 3,7V17A1,1 0 0,0 4,18H16A1,1 0 0,0 17,17V13.5L21,17.5V6.5L17,10.5Z";
        public const string SidebarToggle = "M4,4 H20 V20 H4 Z M6,6 H9 V18 H6 Z M11,6 H18 V18 H11 Z";

        public static System.Windows.Shapes.Path GetIcon(string geometryData, Brush brush, double size = 16)
        {
            return new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(geometryData),
                Fill = brush,
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform
            };
        }
    }
}
