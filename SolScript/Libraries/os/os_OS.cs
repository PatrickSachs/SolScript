using System;
using System.Text;
using JetBrains.Annotations;
using SolScript.Exceptions;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;
using SolScript.Utility;

// ReSharper disable InconsistentNaming

namespace SolScript.Libraries.os
{
    [SolTypeDescriptor(os.NAME, SolTypeMode.Singleton, typeof(os_OS))]
    [SolLibraryName(TYPE)]
    [PublicAPI]
    public class os_OS
    {
        [SolVisibility(false)] public const string TYPE = "OS";

        private static readonly SolString Str_ticks = SolString.ValueOf("ticks").Intern();
        private static readonly SolString Str_milliecond = SolString.ValueOf("millisecond").Intern();
        private static readonly SolString Str_millieconds = SolString.ValueOf("milliseconds").Intern();
        private static readonly SolString Str_second = SolString.ValueOf("second").Intern();
        private static readonly SolString Str_seconds = SolString.ValueOf("seconds").Intern();
        private static readonly SolString Str_minute = SolString.ValueOf("minute").Intern();
        private static readonly SolString Str_minutes = SolString.ValueOf("minutes").Intern();
        private static readonly SolString Str_hour = SolString.ValueOf("hour").Intern();
        private static readonly SolString Str_hours = SolString.ValueOf("hours").Intern();
        private static readonly SolString Str_day = SolString.ValueOf("day").Intern();
        private static readonly SolString Str_days = SolString.ValueOf("days").Intern();
        private static readonly SolString Str_month = SolString.ValueOf("month").Intern();
        private static readonly SolString Str_year = SolString.ValueOf("year").Intern();
        private static readonly SolString Str_day_type = SolString.ValueOf("day_type").Intern();
        private static readonly SolString Str_type = SolString.ValueOf("type").Intern();
        private static readonly SolString Str_update = SolString.ValueOf("update").Intern();
        private static readonly SolString Str_name = SolString.ValueOf("name").Intern();
        private static readonly SolNumber Num_os_unix = new SolNumber(0);
        private static readonly SolNumber Num_os_window = new SolNumber(1);
        private static readonly SolNumber Num_os_macosx = new SolNumber(3);
        private static readonly SolNumber Num_os_xbox = new SolNumber(4);

        /// <summary>
        ///     Gets a table holding information about the current data.
        /// </summary>
        /// <returns>The date table.</returns>
        /// <remarks>
        ///     Available table fields: <c>year, month, day, hour, minute, second, millisecond, ticks, day_type</c><br />The
        ///     day_type fields holds an integer value between 0 and 6, where 0 stands for a Monday and 6 a Sunday.
        /// </remarks>
        public SolTable get_date()
        {
            return ToTable(DateTime.Now);
        }

        public SolTable difftime(SolExecutionContext context, SolTable date1, SolTable date2)
        {
            DateTime dt1;
            DateTime dt2;
            try {
                dt1 = ToDateTime(date1);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Invalid date time table for first date.", ex);
            }
            try {
                dt2 = ToDateTime(date2);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Invalid date time table for second date.", ex);
            }
            TimeSpan span = dt1 - dt2;
            return new SolTable {
                [Str_days] = new SolNumber(span.TotalDays),
                [Str_hours] = new SolNumber(span.TotalHours),
                [Str_minutes] = new SolNumber(span.TotalMinutes),
                [Str_seconds] = new SolNumber(span.TotalSeconds),
                [Str_millieconds] = new SolNumber(span.TotalMilliseconds),
                [Str_ticks] = new SolNumber(span.Ticks)
            };
        }

        /// <summary>
        ///     Retrieves information about the current operating system.
        /// </summary>
        /// <returns>The os table.</returns>
        /// <remarks>Available table fields: <c>type, name, update</c></remarks>
        public SolTable get_info()
        {
            // todo: add more stuff later on such as bits, personalized name, etc.
            OperatingSystem system = Environment.OSVersion;
            SolNumber osType;
            switch (system.Platform) {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                case PlatformID.WinCE:
                    osType = Num_os_window;
                    break;
                case PlatformID.Unix:
                    osType = Num_os_unix;
                    break;
                case PlatformID.Xbox:
                    osType = Num_os_xbox;
                    break;
                case PlatformID.MacOSX:
                    osType = Num_os_macosx;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            SolTable systemTable = new SolTable {
                [Str_type] = osType,
                [Str_name] = SolString.ValueOf(system.VersionString),
                [Str_update] = SolString.ValueOf(system.ServicePack)
            };
            return systemTable;
        }

        #region Native Helpers

        /// <exception cref="SolVariableException">The table is no valid <see cref="DateTime" />.</exception>
        private static DateTime ToDateTime(SolTable table)
        {
            SolValue year;
            SolValue month;
            SolValue day;
            if (!table.TryGet((SolValue) Str_year, out year)) {
                throw new SolVariableException(SolSourceLocation.Native(), "No year is defined. A date-table at least requires a year, month and day field.");
            }
            int yearInt = MakeInteger(year, "Invalid year type. {0}");
            if (!table.TryGet((SolValue) Str_month, out month)) {
                throw new SolVariableException(SolSourceLocation.Native(), "No month is defined. A date-table at least requires a year, month and day field.");
            }
            int monthInt = MakeInteger(month, "Invalid month type. {0}");
            if (!table.TryGet((SolValue) Str_day, out day)) {
                throw new SolVariableException(SolSourceLocation.Native(), "No day is defined. A date-table at least requires a year, month and day field.");
            }
            int dayInt = MakeInteger(day, "Invalid day type. {0}");
            SolValue hour;
            SolValue minute;
            SolValue second;
            SolValue millisecond;
            hour = table.TryGet((SolValue) Str_hour, out hour) ? hour : null;
            minute = table.TryGet((SolValue) Str_minute, out minute) ? minute : null;
            second = table.TryGet((SolValue) Str_second, out second) ? second : null;
            millisecond = table.TryGet((SolValue) Str_milliecond, out millisecond) ? millisecond : null;
            if (hour == null && minute == null && second == null) {
                if (millisecond != null) {
                    throw new SolVariableException(SolSourceLocation.Native(), "Cannot specify a millisecond if hour, minute and second are not specified.");
                }
                return new DateTime(yearInt, monthInt, dayInt);
            }
            if (hour != null && minute != null && second != null) {
                int hourInt = MakeInteger(hour, "Invalid hour type. {0}");
                int minuteInt = MakeInteger(minute, "Invalid minute type. {0}");
                int secondInt = MakeInteger(second, "Invalid second type. {0}");
                if (millisecond == null) {
                    return new DateTime(yearInt, monthInt, dayInt, hourInt, minuteInt, secondInt);
                }
                int msInt = MakeInteger(minute, "Invalid millisecond type. {0}");
                return new DateTime(yearInt, monthInt, dayInt, hourInt, minuteInt, secondInt, msInt);
            }
            StringBuilder builder = new StringBuilder("If any of the hour, minute or second fields are specified, all of them must be specified. Specified were:");
            bool first = true;
            if (hour != null) {
                builder.Append(" hour");
                first = false;
            }
            if (minute != null) {
                if (!first) {
                    builder.Append(',');
                }
                builder.Append(" minute");
                first = false;
            }
            if (second != null) {
                if (!first) {
                    builder.Append(',');
                }
                builder.Append(" second");
            }
            throw new SolVariableException(SolSourceLocation.Native(), builder.ToString());
        }

        /// <exception cref="SolVariableException">Invalid type/not an integer.</exception>
        private static int MakeInteger(SolValue value, string error)
        {
            SolNumber number = AssertTypeAndCast<SolNumber>(value, SolNumber.TYPE, error);
            int integer;
            if (!InternalHelper.NumberToInteger(number, out integer)) {
                throw new SolVariableException(SolSourceLocation.Native(), string.Format(error, "The number must be an integer, but contained a decimal part."));
            }
            return integer;
        }

        /// <exception cref="SolVariableException">Invalid type.</exception>
        private static T AssertTypeAndCast<T>(SolValue value, string type, string error) where T : SolValue
        {
            if (value == null) {
                throw new SolVariableException(SolSourceLocation.Native(), string.Format(error, "The internal value is null."));
            }
            if (value.Type != type) {
                throw new SolVariableException(SolSourceLocation.Native(), string.Format(error, "The value is of type \"" + value.Type + "\", but was expected to be of type \"" + type + "\"."));
            }
            return (T) value;
        }

        private static SolTable ToTable(DateTime time)
        {
            return new SolTable {
                [Str_year] = new SolNumber(time.Year),
                [Str_month] = new SolNumber(time.Month),
                [Str_day] = new SolNumber(time.Day),
                [Str_hour] = new SolNumber(time.Hour),
                [Str_minute] = new SolNumber(time.Minute),
                [Str_second] = new SolNumber(time.Second),
                [Str_milliecond] = new SolNumber(time.Millisecond),
                [Str_ticks] = new SolNumber(time.Ticks),
                [Str_day_type] = new SolNumber((int) time.DayOfWeek)
            };
        }

        #endregion
    }
}