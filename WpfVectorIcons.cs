using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EternNotes
{
    public static class WpfVectorIcons
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
        public const string Code = "M9.4,16.6L4.8,12L9.4,7.4L8,6L2,12L8,18L9.4,16.6M14.6,16.6L19.2,12L14.6,7.4L16,6L22,12L16,18L14.6,16.6Z";
        public const string Star = "M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z";
        public const string Rocket = "M13.13,22.19L11.5,18.36L8.14,21.72L7.43,21L8.14,17.64L4.31,16L5.5,11.5C6.7,8.23 9.47,5.55 12.87,4.5C14.77,3.15 17.15,2.37 19.64,2.23C19.78,4.72 19,7.1 17.65,9C16.6,12.4 13.92,15.17 10.65,16.37L13.13,22.19M15.5,8A1.5,1.5 0 0,0 14,6.5A1.5,1.5 0 0,0 12.5,8A1.5,1.5 0 0,0 14,9.5A1.5,1.5 0 0,0 15.5,8Z";
        public const string Terminal = "M20,4H4A2,2 0 0,0 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V6A2,2 0 0,0 20,4M7.5,15L6.08,13.58L9.5,10.16L6.08,6.74L7.5,5.32L12.33,10.16L7.5,15M17,15H11V13H17V15Z";
        public const string Cpu = "M6,2H8V4H16V2H18V4H20A2,2 0 0,1 22,6V8H24V10H22V14H24V16H22V18A2,2 0 0,1 20,20H18V22H16V20H8V22H6V20H4A2,2 0 0,1 2,18V16H0V14H2V10H0V8H2V6A2,2 0 0,1 4,4H6V2M8,8V16H16V8H8M4,6V18H6V6H4M18,6V18H20V6H18Z";
        public const string Lightning = "M7,2V11H3L17,22V13H21L7,2Z";
        public const string Database = "M12,3C7.58,3 4,4.79 4,7V17C4,19.21 7.58,21 12,21C16.42,21 20,19.21 20,17V7C20,4.79 16.42,3 12,3M18,17C18,17.5 15.31,19 12,19C8.69,19 6,17.5 6,17V14.77C7.72,15.55 9.77,16 12,16C14.23,16 16.28,15.55 18,14.77V17M18,12C18,12.5 15.31,14 12,14C8.69,14 6,12.5 6,12V9.77C7.72,10.55 9.77,11 12,11C14.23,11 16.28,10.55 18,9.77V12M12,5C15.31,5 18,6.5 18,7C18,7.5 15.31,9 12,9C8.69,9 6,7.5 6,7C6,6.5 8.69,5 12,5Z";
        public const string Palette = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A2.5,2.5 0 0,0 14.5,19.5C14.5,19.12 14.35,18.78 14.11,18.5C13.87,18.22 13.73,17.86 13.73,17.5A1.27,1.27 0 0,1 15,16.23H16.5A5.5,5.5 0 0,0 22,10.73C22,5.9 17.5,2 12,2M6.5,12A1.5,1.5 0 0,1 5,10.5A1.5,1.5 0 0,1 6.5,9A1.5,1.5 0 0,1 8,10.5A1.5,1.5 0 0,1 6.5,12M9.5,8A1.5,1.5 0 0,1 8,6.5A1.5,1.5 0 0,1 9.5,5A1.5,1.5 0 0,1 11,6.5A1.5,1.5 0 0,1 9.5,8M14.5,8A1.5,1.5 0 0,1 13,6.5A1.5,1.5 0 0,1 14.5,5A1.5,1.5 0 0,1 16,6.5A1.5,1.5 0 0,1 14.5,8M17.5,12A1.5,1.5 0 0,1 16,10.5A1.5,1.5 0 0,1 17.5,9A1.5,1.5 0 0,1 19,10.5A1.5,1.5 0 0,1 17.5,12Z";
        public const string Layers = "M12,16.54L8.46,13.8L7.04,14.91L12,18.77L16.96,14.91L15.54,13.8L12,16.54M12,2.53L3.5,9.11L12,15.69L20.5,9.11L12,2.53M12,4.82L17.57,9.11L12,13.4L6.43,9.11L12,4.82Z";
        public const string Globe = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M11,19.93C7.05,19.44 4,16.08 4,12C4,11.38 4.08,10.78 4.21,10.21L9,15V16A2,2 0 0,0 11,18V19.93M17.9,17.39C17.64,16.58 16.9,16 16,16H15V13A1,1 0 0,0 14,12H8V10H10A1,1 0 0,0 11,9V7H13A2,2 0 0,0 15,5V4.59C17.93,5.77 20,8.64 20,12C20,14.08 19.2,15.97 17.9,17.39Z";
        public const string Book = "M18,2A2,2 0 0,1 20,4V20A2,2 0 0,1 18,22H6C4.89,22 4,21.1 4,20V4C4,2.89 4.89,2 6,2H18M6,4V20H18V4H6Z";
        public const string Music = "M12,3V13.55C11.41,13.21 10.73,13 10,13A5,5 0 0,0 5,18A5,5 0 0,0 10,23A5,5 0 0,0 15,18V6H19V3H12Z";
        public const string SidebarToggle = "M4,4 H20 V20 H4 Z M6,6 H9 V18 H6 Z M11,6 H18 V18 H11 Z";

        public static string GetIconGeometry(string iconType)
        {
            if (string.IsNullOrEmpty(iconType)) return Folder;
            switch (iconType.ToLowerInvariant())
            {
                case "gamepad": return Gamepad;
                case "video": return Video;
                case "code": return Code;
                case "star": return Star;
                case "rocket": return Rocket;
                case "terminal": return Terminal;
                case "cpu": return Cpu;
                case "lightning": return Lightning;
                case "database": return Database;
                case "palette": return Palette;
                case "layers": return Layers;
                case "globe": return Globe;
                case "book": return Book;
                case "music": return Music;
                default: return Folder;
            }
        }

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
