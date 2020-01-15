using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace GithubUpdater
{
    public class Version
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Revision { get; set; }

        public Version(int major)
        {
            Major = major;
        }

        public Version(int major, int minor) : this(major)
        {
            Minor = minor;
        }

        public Version(int major, int minor, int revision) : this(major, minor)
        {
            Revision = revision;
        }

        public static bool operator >(Version first, Version second)
        {
            if (first.Major > second.Major)
                return true;
            else if (first.Minor > second.Minor)
                return true;
            else if (first.Revision > second.Revision)
                return true;

            return false;
        }

        public static bool operator <(Version first, Version second)
        {
            if (first.Major < second.Major)
                return true;
            else if (first.Minor < second.Minor)
                return true;
            else if (first.Revision < second.Revision)
                return true;

            return false;
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Revision}";
        }

        public static Version ConvertToVersion(string version)
        {
            //^\d{1,3}\.\d{1,3}(?:\.\d{1,6})?$
            version = version.Replace("v", "", true, CultureInfo.InvariantCulture);

            Regex regex = new Regex(@"\d+(?:\.\d+)+");

            if (regex.IsMatch(version))
            {
                var splitted = version.Split('.').Select(int.Parse).ToArray();
                if (splitted.Length == 1)
                {
                    return new Version(splitted[0]);
                }
                else if (splitted.Length == 2)
                {
                    return new Version(splitted[0], splitted[1]);

                }
                else if (splitted.Length >= 3)
                {
                    return new Version(splitted[0], splitted[1], splitted[2]);
                }
            }

            throw new FormatException("Version was in a invalid format");
        }
    }
}
