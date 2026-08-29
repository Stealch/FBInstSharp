using FBINSTSharp.Core.Models;
using System;

namespace FBINSTSharp.Core.Parsers
{
    public static class FormatCommandParser
    {
        public static FormatOptions Parse(string[] args)
        {
            var options = new FormatOptions();
            string devicePath = "";

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg.StartsWith("--") || arg.StartsWith("-"))
                {
                    switch (arg)
                    {
                        case "--force":
                        case "-f":
                            options.Force = true;
                            break;

                        case "--raw":
                        case "-r":
                            options.Raw = true;
                            break;

                        case "--zip":
                        case "-z":
                            options.Zip = true;
                            break;

                        case "--align":
                        case "-a":
                            options.Align = true;
                            break;

                        case "--chs":
                            options.ChsMode = true;
                            break;

                        case "--fat32":
                            options.IsFat32 = true;
                            options.IsFat16 = false;
                            options.IsFileSystemExplicit = true;
                            break;

                        case "--fat16":
                            options.IsFat32 = false;
                            options.IsFat16 = true;
                            options.IsFileSystemExplicit = true;
                            break;

                        case "--size":
                        case "-s":
                            if (i + 1 >= args.Length)
                                throw new ArgumentException("--size requires a parameter");
                            options.PartitionSize = ParseSectorCount(args[++i]);
                            break;

                        case "--primary":
                        case "-p":
                            if (i + 1 >= args.Length)
                                throw new ArgumentException("--primary requires a parameter");
                            ulong primaryVal = ParseSectorCount(args[++i]);
                            if (primaryVal < 63UL * 256)
                                throw new ArgumentException($"primary data size {primaryVal} is too small (minimum {63 * 256})");
                            options.PrimarySize = primaryVal;
                            break;

                        case "--extended":
                        case "-e":
                            if (i + 1 >= args.Length)
                                throw new ArgumentException("--extended requires a parameter");
                            options.ExtendedSize = ParseSectorCount(args[++i]);
                            break;

                        case "--base":
                        case "-b":
                            if (i + 1 >= args.Length)
                                throw new ArgumentException("--base requires a parameter");
                            options.BaseSector = int.Parse(args[++i]);
                            break;

                        case "--list-size":
                        case "-l":
                            if (i + 1 >= args.Length)
                                throw new ArgumentException("--list-size requires a parameter");
                            options.ListSize = int.Parse(args[++i]);
                            break;

                        case "--unit-size":
                        case "-u":
                            if (i + 1 >= args.Length)
                                throw new ArgumentException("--unit-size requires a parameter");
                            options.UnitSize = int.Parse(args[++i]);
                            break;

                        case "--nalign":
                        case "-n":
                            if (i + 1 >= args.Length)
                                throw new ArgumentException("--nalign requires a parameter");
                            options.NandAlign = int.Parse(args[++i]) - 1;
                            if (options.NandAlign < 255 || ((options.NandAlign + 1) & options.NandAlign) != 0)
                                throw new ArgumentException($"invalid alignment value {options.NandAlign + 1}");
                            break;

                        case "--max-sectors":
                            if (i + 1 >= args.Length)
                                throw new ArgumentException("--max-sectors requires a parameter");
                            options.MaxSectors = int.Parse(args[++i]);
                            if (options.MaxSectors < 0 || options.MaxSectors > 127)
                                throw new ArgumentException($"invalid max sectors value {options.MaxSectors}");
                            break;

                        case "--archive":
                            if (i + 1 >= args.Length)
                                throw new ArgumentException("--archive requires a parameter");
                            options.ArchiveFile = args[++i];
                            break;

                        default:
                            throw new ArgumentException($"invalid option {arg} for format");
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(devicePath))
                        throw new ArgumentException($"unexpected argument '{arg}', device already set to '{devicePath}'");
                    devicePath = arg;
                }
            }

            if (string.IsNullOrEmpty(devicePath))
                throw new ArgumentException("device not specified");

            options.DevicePath = devicePath;

            if (options.ListSize == 0)
                options.ListSize = 0x80000 * 510;

            if (options.PrimarySize == 0)
                options.PrimarySize = 63UL * 256;

            return options;
        }

        private static ulong ParseSectorCount(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("invalid size");

            value = value.ToLowerInvariant();
            int suffixPos = -1;
            ulong multiplier = 1;

            if (value.EndsWith("k"))
            {
                multiplier = 2;
                suffixPos = value.Length - 1;
            }
            else if (value.EndsWith("m"))
            {
                multiplier = 2048;
                suffixPos = value.Length - 1;
            }
            else if (value.EndsWith("g"))
            {
                multiplier = 2UL * 1024 * 1024;
                suffixPos = value.Length - 1;
            }

            string numPart = suffixPos > 0 ? value.Substring(0, suffixPos) : value;
            if (!ulong.TryParse(numPart, out ulong result))
                throw new ArgumentException($"invalid numeric value '{numPart}'");

            return result * multiplier;
        }
    }
}