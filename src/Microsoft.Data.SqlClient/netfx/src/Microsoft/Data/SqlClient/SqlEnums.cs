// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.Data.Common;
using Microsoft.Data.SqlClient.Server;

namespace Microsoft.Data.SqlClient
{
    internal sealed class MetaType
    {
        internal readonly Type ClassType;   // com+ type
        internal readonly Type SqlType;

        internal readonly int FixedLength; // fixed length size in bytes (-1 for variable)
        internal readonly bool IsFixed;     // true if fixed length, note that sqlchar and sqlbinary are not considered fixed length
        internal readonly bool IsLong;      // true if long
        internal readonly bool IsPlp;       // Column is Partially Length Prefixed (MAX)
        internal readonly byte Precision;   // maxium precision for numeric types // $CONSIDER - are we going to use this?
        internal readonly byte Scale;
        internal readonly byte TDSType;
        internal readonly byte NullableType;

        internal readonly string TypeName;    // string name of this type
        internal readonly SqlDbType SqlDbType;
        internal readonly DbType DbType;

        //  holds count of property bytes expected for a SQLVariant structure
        internal readonly byte PropBytes;


        // pre-computed fields
        internal readonly bool IsAnsiType;
        internal readonly bool IsBinType;
        internal readonly bool IsCharType;
        internal readonly bool IsNCharType;
        internal readonly bool IsSizeInCharacters;
        internal readonly bool IsNewKatmaiType;
        internal readonly bool IsVarTime;

        internal readonly bool Is70Supported;
        internal readonly bool Is80Supported;
        internal readonly bool Is90Supported;
        internal readonly bool Is100Supported;

        public MetaType(byte precision, byte scale, int fixedLength, bool isFixed, bool isLong, bool isPlp, byte tdsType, byte nullableTdsType, string typeName, Type classType, Type sqlType, SqlDbType sqldbType, DbType dbType, byte propBytes)
        {
            this.Precision = precision;
            this.Scale = scale;
            this.FixedLength = fixedLength;
            this.IsFixed = isFixed;
            this.IsLong = isLong;
            this.IsPlp = isPlp;
            // can we get rid of this (?just have a mapping?)
            this.TDSType = tdsType;
            this.NullableType = nullableTdsType;
            this.TypeName = typeName;
            this.SqlDbType = sqldbType;
            this.DbType = dbType;

            this.ClassType = classType;
            this.SqlType = sqlType;
            this.PropBytes = propBytes;

            IsAnsiType = _IsAnsiType(sqldbType);
            IsBinType = _IsBinType(sqldbType);
            IsCharType = _IsCharType(sqldbType);
            IsNCharType = _IsNCharType(sqldbType);
            IsSizeInCharacters = _IsSizeInCharacters(sqldbType);
            IsNewKatmaiType = _IsNewKatmaiType(sqldbType);
            IsVarTime = _IsVarTime(sqldbType);

            Is70Supported = _Is70Supported(SqlDbType);
            Is80Supported = _Is80Supported(SqlDbType);
            Is90Supported = _Is90Supported(SqlDbType);
            Is100Supported = _Is100Supported(SqlDbType);
        }

        // properties should be inlined so there should be no perf penalty for using these accessor functions
        public int TypeId
        {             // partial length prefixed (xml, nvarchar(max),...)
            get { return 0; }
        }

        private static bool _IsAnsiType(SqlDbType type)
        {
            return (type == SqlDbType.Char ||
                   type == SqlDbType.VarChar ||
                   type == SqlDbType.Text);
        }

        // is this type size expressed as count of characters or bytes?
        private static bool _IsSizeInCharacters(SqlDbType type)
        {
            return (type == SqlDbType.NChar ||
                   type == SqlDbType.NVarChar ||
                   type == SqlDbType.Xml ||
                   type == SqlDbType.NText);
        }

        private static bool _IsCharType(SqlDbType type)
        {
            return (type == SqlDbType.NChar ||
                   type == SqlDbType.NVarChar ||
                   type == SqlDbType.NText ||
                   type == SqlDbType.Char ||
                   type == SqlDbType.VarChar ||
                   type == SqlDbType.Text ||
                   type == SqlDbType.Xml);
        }

        private static bool _IsNCharType(SqlDbType type)
        {
            return (type == SqlDbType.NChar ||
                   type == SqlDbType.NVarChar ||
                   type == SqlDbType.NText ||
                   type == SqlDbType.Xml);
        }

        private static bool _IsBinType(SqlDbType type)
        {
            return (type == SqlDbType.Image ||
                   type == SqlDbType.Binary ||
                   type == SqlDbType.VarBinary ||
                   type == SqlDbType.Timestamp ||
                   type == SqlDbType.Udt ||
                   (int)type == 24 /*SqlSmallVarBinary*/);
        }

        private static bool _Is70Supported(SqlDbType type)
        {
            return ((type != SqlDbType.BigInt) && ((int)type > 0) &&
                   ((int)type <= (int)SqlDbType.VarChar));
        }

        private static bool _Is80Supported(SqlDbType type)
        {
            return ((int)type >= 0 &&
                ((int)type <= (int)SqlDbType.Variant));
        }

        private static bool _Is90Supported(SqlDbType type)
        {
            return _Is80Supported(type) ||
                    SqlDbType.Xml == type ||
                    SqlDbType.Udt == type;
        }

        private static bool _Is100Supported(SqlDbType type)
        {
            return _Is90Supported(type) ||
                    SqlDbType.Date == type ||
                    SqlDbType.Time == type ||
                    SqlDbType.DateTime2 == type ||
                    SqlDbType.DateTimeOffset == type;
        }

        private static bool _IsNewKatmaiType(SqlDbType type)
        {
            return SqlDbType.Structured == type;
        }

        internal static bool _IsVarTime(SqlDbType type)
        {
            return (type == SqlDbType.Time || type == SqlDbType.DateTime2 || type == SqlDbType.DateTimeOffset);
        }

        //
        // map SqlDbType to MetaType class
        //
        internal static MetaType GetMetaTypeFromSqlDbType(SqlDbType target, bool isMultiValued)
        { // WebData 113289
            switch (target)
            {
                case SqlDbType.BigInt:
                    return MetaBigInt;
                case SqlDbType.Binary:
                    return MetaBinary;
                case SqlDbType.Bit:
                    return MetaBit;
                case SqlDbType.Char:
                    return MetaChar;
                case SqlDbType.DateTime:
                    return MetaDateTime;
                case SqlDbType.Decimal:
                    return MetaDecimal;
                case SqlDbType.Float:
                    return MetaFloat;
                case SqlDbType.Image:
                    return MetaImage;
                case SqlDbType.Int:
                    return MetaInt;
                case SqlDbType.Money:
                    return MetaMoney;
                case SqlDbType.NChar:
                    return MetaNChar;
                case SqlDbType.NText:
                    return MetaNText;
                case SqlDbType.NVarChar:
                    return MetaNVarChar;
                case SqlDbType.Real:
                    return MetaReal;
                case SqlDbType.UniqueIdentifier:
                    return MetaUniqueId;
                case SqlDbType.SmallDateTime:
                    return MetaSmallDateTime;
                case SqlDbType.SmallInt:
                    return MetaSmallInt;
                case SqlDbType.SmallMoney:
                    return MetaSmallMoney;
                case SqlDbType.Text:
                    return MetaText;
                case SqlDbType.Timestamp:
                    return MetaTimestamp;
                case SqlDbType.TinyInt:
                    return MetaTinyInt;
                case SqlDbType.VarBinary:
                    return MetaVarBinary;
                case SqlDbType.VarChar:
                    return MetaVarChar;
                case SqlDbType.Variant:
                    return MetaVariant;
                case (SqlDbType)TdsEnums.SmallVarBinary:
                    return MetaSmallVarBinary;
                case SqlDbType.Xml:
                    return MetaXml;
                case SqlDbType.Udt:
                    return MetaUdt;
                case SqlDbType.Structured:
                    if (isMultiValued)
                    {
                        return MetaTable;
                    }
                    else
                    {
                        return MetaSUDT;
                    }
                case SqlDbType.Date:
                    return MetaDate;
                case SqlDbType.Time:
                    return MetaTime;
                case SqlDbType.DateTime2:
                    return MetaDateTime2;
                case SqlDbType.DateTimeOffset:
                    return MetaDateTimeOffset;
                default:
                    throw SQL.InvalidSqlDbType(target);
            }
        }

        //
        // map DbType to MetaType class
        //
        internal static MetaType GetMetaTypeFromDbType(DbType target)
        {
            // if we can't map it, we need to throw
            switch (target)
            {
                case DbType.AnsiString:
                    return MetaVarChar;
                case DbType.AnsiStringFixedLength:
                    return MetaChar;
                case DbType.Binary:
                    return MetaVarBinary;
                case DbType.Byte:
                    return MetaTinyInt;
                case DbType.Boolean:
                    return MetaBit;
                case DbType.Currency:
                    return MetaMoney;
                case DbType.Date:
                case DbType.DateTime:
                    return MetaDateTime;
                case DbType.Decimal:
                    return MetaDecimal;
                case DbType.Double:
                    return MetaFloat;
                case DbType.Guid:
                    return MetaUniqueId;
                case DbType.Int16:
                    return MetaSmallInt;
                case DbType.Int32:
                    return MetaInt;
                case DbType.Int64:
                    return MetaBigInt;
                case DbType.Object:
                    return MetaVariant;
                case DbType.Single:
                    return MetaReal;
                case DbType.String:
                    return MetaNVarChar;
                case DbType.StringFixedLength:
                    return MetaNChar;
                case DbType.Time:
                    return MetaDateTime;
                case DbType.Xml:
                    return MetaXml;
                case DbType.DateTime2:
                    return MetaDateTime2;
                case DbType.DateTimeOffset:
                    return MetaDateTimeOffset;
                case DbType.SByte:                  // unsupported
                case DbType.UInt16:
                case DbType.UInt32:
                case DbType.UInt64:
                case DbType.VarNumeric:
                default:
                    throw ADP.DbTypeNotSupported(target, typeof(SqlDbType)); // no direct mapping, error out
            }
        }

        internal static MetaType GetMaxMetaTypeFromMetaType(MetaType mt)
        {
            // if we can't map it, we need to throw
            switch (mt.SqlDbType)
            {
                case SqlDbType.VarBinary:
                case SqlDbType.Binary:
                    return MetaMaxVarBinary;
                case SqlDbType.VarChar:
                case SqlDbType.Char:
                    return MetaMaxVarChar;
                case SqlDbType.NVarChar:
                case SqlDbType.NChar:
                    return MetaMaxNVarChar;
                case SqlDbType.Udt:
                    // TODO: we should probably verify we are hitting WinFS, otherwise this is invalid
                    return MetaMaxUdt;
                default:
                    return mt;
            }
        }

        //
        // map COM+ Type to MetaType class
        //
        static internal MetaType GetMetaTypeFromType(Type dataType)
        {
            return GetMetaTypeFromValue(dataType, null, false, true);
        }
        static internal MetaType GetMetaTypeFromValue(object value, bool streamAllowed = true)
        {
            return GetMetaTypeFromValue(value.GetType(), value, true, streamAllowed);
        }

        static private MetaType GetMetaTypeFromValue(Type dataType, object value, bool inferLen, bool streamAllowed)
        {
            switch (Type.GetTypeCode(dataType))
            {
                case TypeCode.Empty:
                    throw ADP.InvalidDataType(TypeCode.Empty);
                case TypeCode.Object:
                    if (dataType == typeof(System.Byte[]))
                    {
                        // mdac 90455 must not default to image if inferLen is false ...
                        //
                        if (!inferLen || ((byte[])value).Length <= TdsEnums.TYPE_SIZE_LIMIT)
                        {
                            return MetaVarBinary;
                        }
                        else
                        {
                            return MetaImage;
                        }
                    }
                    else if (dataType == typeof(System.Guid))
                    {
                        return MetaUniqueId;
                    }
                    else if (dataType == typeof(System.Object))
                    {
                        return MetaVariant;
                    } // check sql types now
                    else if (dataType == typeof(SqlBinary))
                        return MetaVarBinary;
                    else if (dataType == typeof(SqlBoolean))
                        return MetaBit;
                    else if (dataType == typeof(SqlByte))
                        return MetaTinyInt;
                    else if (dataType == typeof(SqlBytes))
                        return MetaVarBinary;
                    else if (dataType == typeof(SqlChars))
                        return MetaNVarChar; // MDAC 87587
                    else if (dataType == typeof(SqlDateTime))
                        return MetaDateTime;
                    else if (dataType == typeof(SqlDouble))
                        return MetaFloat;
                    else if (dataType == typeof(SqlGuid))
                        return MetaUniqueId;
                    else if (dataType == typeof(SqlInt16))
                        return MetaSmallInt;
                    else if (dataType == typeof(SqlInt32))
                        return MetaInt;
                    else if (dataType == typeof(SqlInt64))
                        return MetaBigInt;
                    else if (dataType == typeof(SqlMoney))
                        return MetaMoney;
                    else if (dataType == typeof(SqlDecimal))
                        return MetaDecimal;
                    else if (dataType == typeof(SqlSingle))
                        return MetaReal;
                    else if (dataType == typeof(SqlXml))
                        return MetaXml;
                    else if (dataType == typeof(SqlString))
                    {
                        return ((inferLen && !((SqlString)value).IsNull) ? PromoteStringType(((SqlString)value).Value) : MetaNVarChar); // MDAC 87587
                    }
                    else if (dataType == typeof(IEnumerable<DbDataRecord>) || dataType == typeof(DataTable))
                    {
                        return MetaTable;
                    }
                    else if (dataType == typeof(TimeSpan))
                    {
                        return MetaTime;
                    }
                    else if (dataType == typeof(DateTimeOffset))
                    {
                        return MetaDateTimeOffset;
                    }
                    else
                    {
                        // UDT ?
                        SqlUdtInfo attribs = SqlUdtInfo.TryGetFromType(dataType);
                        if (attribs != null)
                        {
                            return MetaUdt;
                        }
                        if (streamAllowed)
                        {
                            // Derived from Stream ?
                            if (typeof(Stream).IsAssignableFrom(dataType))
                            {
                                return MetaVarBinary;
                            }
                            // Derived from TextReader ?
                            if (typeof(TextReader).IsAssignableFrom(dataType))
                            {
                                return MetaNVarChar;
                            }
                            // Derived from XmlReader ? 
                            if (typeof(System.Xml.XmlReader).IsAssignableFrom(dataType))
                            {
                                return MetaXml;
                            }
                        }
                    }
                    throw ADP.UnknownDataType(dataType);

                case TypeCode.DBNull:
                    throw ADP.InvalidDataType(TypeCode.DBNull);
                case TypeCode.Boolean:
                    return MetaBit;
                case TypeCode.Char:
                    throw ADP.InvalidDataType(TypeCode.Char);
                case TypeCode.SByte:
                    throw ADP.InvalidDataType(TypeCode.SByte);
                case TypeCode.Byte:
                    return MetaTinyInt;
                case TypeCode.Int16:
                    return MetaSmallInt;
                case TypeCode.UInt16:
                    throw ADP.InvalidDataType(TypeCode.UInt16);
                case TypeCode.Int32:
                    return MetaInt;
                case TypeCode.UInt32:
                    throw ADP.InvalidDataType(TypeCode.UInt32);
                case TypeCode.Int64:
                    return MetaBigInt;
                case TypeCode.UInt64:
                    throw ADP.InvalidDataType(TypeCode.UInt64);
                case TypeCode.Single:
                    return MetaReal;
                case TypeCode.Double:
                    return MetaFloat;
                case TypeCode.Decimal:
                    return MetaDecimal;
                case TypeCode.DateTime:
                    return MetaDateTime;
                case TypeCode.String:
                    return (inferLen ? PromoteStringType((string)value) : MetaNVarChar);
                default:
                    throw ADP.UnknownDataTypeCode(dataType, Type.GetTypeCode(dataType));
            }
        }

        internal static object GetNullSqlValue(Type sqlType)
        {
            if (sqlType == typeof(SqlSingle))
                return SqlSingle.Null;
            else if (sqlType == typeof(SqlString))
                return SqlString.Null;
            else if (sqlType == typeof(SqlDouble))
                return SqlDouble.Null;
            else if (sqlType == typeof(SqlBinary))
                return SqlBinary.Null;
            else if (sqlType == typeof(SqlGuid))
                return SqlGuid.Null;
            else if (sqlType == typeof(SqlBoolean))
                return SqlBoolean.Null;
            else if (sqlType == typeof(SqlByte))
                return SqlByte.Null;
            else if (sqlType == typeof(SqlInt16))
                return SqlInt16.Null;
            else if (sqlType == typeof(SqlInt32))
                return SqlInt32.Null;
            else if (sqlType == typeof(SqlInt64))
                return SqlInt64.Null;
            else if (sqlType == typeof(SqlDecimal))
                return SqlDecimal.Null;
            else if (sqlType == typeof(SqlDateTime))
                return SqlDateTime.Null;
            else if (sqlType == typeof(SqlMoney))
                return SqlMoney.Null;
            else if (sqlType == typeof(SqlXml))
                return SqlXml.Null;
            else if (sqlType == typeof(object))
                return DBNull.Value;
            else if (sqlType == typeof(IEnumerable<DbDataRecord>))
                return DBNull.Value;
            else if (sqlType == typeof(DataTable))
                return DBNull.Value;
            else if (sqlType == typeof(DateTime))
                return DBNull.Value;
            else if (sqlType == typeof(TimeSpan))
                return DBNull.Value;
            else if (sqlType == typeof(DateTimeOffset))
                return DBNull.Value;
            else
            {
                Debug.Assert(false, "Unknown SqlType!");
                return DBNull.Value;
            }
        }

        internal static MetaType PromoteStringType(string s)
        {
            int len = s.Length;

            if ((len << 1) > TdsEnums.TYPE_SIZE_LIMIT)
            {
                return MetaVarChar; // try as var char since we can send a 8K characters
            }
            return MetaNVarChar; // send 4k chars, but send as unicode
        }

        internal static object GetComValueFromSqlVariant(object sqlVal)
        {
            object comVal = null;

            if (ADP.IsNull(sqlVal))
                return comVal;

            if (sqlVal is SqlSingle)
                comVal = ((SqlSingle)sqlVal).Value;
            else if (sqlVal is SqlString)
                comVal = ((SqlString)sqlVal).Value;
            else if (sqlVal is SqlDouble)
                comVal = ((SqlDouble)sqlVal).Value;
            else if (sqlVal is SqlBinary)
                comVal = ((SqlBinary)sqlVal).Value;
            else if (sqlVal is SqlGuid)
                comVal = ((SqlGuid)sqlVal).Value;
            else if (sqlVal is SqlBoolean)
                comVal = ((SqlBoolean)sqlVal).Value;
            else if (sqlVal is SqlByte)
                comVal = ((SqlByte)sqlVal).Value;
            else if (sqlVal is SqlInt16)
                comVal = ((SqlInt16)sqlVal).Value;
            else if (sqlVal is SqlInt32)
                comVal = ((SqlInt32)sqlVal).Value;
            else if (sqlVal is SqlInt64)
                comVal = ((SqlInt64)sqlVal).Value;
            else if (sqlVal is SqlDecimal)
                comVal = ((SqlDecimal)sqlVal).Value;
            else if (sqlVal is SqlDateTime)
                comVal = ((SqlDateTime)sqlVal).Value;
            else if (sqlVal is SqlMoney)
                comVal = ((SqlMoney)sqlVal).Value;
            else if (sqlVal is SqlXml)
                comVal = ((SqlXml)sqlVal).Value;
            else
            {
                AssertIsUserDefinedTypeInstance(sqlVal, "unknown SqlType class stored in sqlVal");
            }


            return comVal;
        }

        /// <summary>
        /// Assert that the supplied object is an instance of a SQL User-Defined Type (UDT).
        /// </summary>
        /// <param name="sqlValue">Object instance to be tested.</param>
        /// <param name="failedAssertMessage"></param>
        /// <remarks>
        /// This method is only compiled with debug builds, and it a helper method for the GetComValueFromSqlVariant method defined in this class.
        /// 
        /// The presence of the SqlUserDefinedTypeAttribute on the object's type 
        /// is used to determine if the object is a UDT instance (if present it is a UDT, else it is not).
        /// </remarks>
        /// <exception cref="NullReferenceException">
        /// If sqlValue is null.  Callers must ensure the object is non-null.
        /// </exception>
        [Conditional("DEBUG")]
        private static void AssertIsUserDefinedTypeInstance(object sqlValue, string failedAssertMessage)
        {
            Type type = sqlValue.GetType();
            Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute[] attributes = (Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute[])type.GetCustomAttributes(typeof(Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute), true);

            Debug.Assert(attributes.Length > 0, failedAssertMessage);
        }

        // devnote: This method should not be used with SqlDbType.Date and SqlDbType.DateTime2. 
        //          With these types the values should be used directly as CLR types instead of being converted to a SqlValue
        internal static object GetSqlValueFromComVariant(object comVal)
        {
            object sqlVal = null;
            if ((null != comVal) && (DBNull.Value != comVal))
            {
                if (comVal is float)
                    sqlVal = new SqlSingle((float)comVal);
                else if (comVal is string)
                    sqlVal = new SqlString((string)comVal);
                else if (comVal is double)
                    sqlVal = new SqlDouble((double)comVal);
                else if (comVal is System.Byte[])
                    sqlVal = new SqlBinary((byte[])comVal);
                else if (comVal is System.Char)
                    sqlVal = new SqlString(((char)comVal).ToString());
                else if (comVal is System.Char[])
                    sqlVal = new SqlChars((System.Char[])comVal);
                else if (comVal is System.Guid)
                    sqlVal = new SqlGuid((Guid)comVal);
                else if (comVal is bool)
                    sqlVal = new SqlBoolean((bool)comVal);
                else if (comVal is byte)
                    sqlVal = new SqlByte((byte)comVal);
                else if (comVal is Int16)
                    sqlVal = new SqlInt16((Int16)comVal);
                else if (comVal is Int32)
                    sqlVal = new SqlInt32((Int32)comVal);
                else if (comVal is Int64)
                    sqlVal = new SqlInt64((Int64)comVal);
                else if (comVal is Decimal)
                    sqlVal = new SqlDecimal((Decimal)comVal);
                else if (comVal is DateTime)
                {
                    // devnote: Do not use with SqlDbType.Date and SqlDbType.DateTime2. See comment at top of method.
                    sqlVal = new SqlDateTime((DateTime)comVal);
                }
                else if (comVal is XmlReader)
                    sqlVal = new SqlXml((XmlReader)comVal);
                else if (comVal is TimeSpan || comVal is DateTimeOffset)
                    sqlVal = comVal;
#if DEBUG
                else
                    Debug.Assert(false, "unknown SqlType class stored in sqlVal");
#endif
            }
            return sqlVal;
        }

        internal static SqlDbType GetSqlDbTypeFromOleDbType(short dbType, string typeName)
        {
            SqlDbType sqlType = SqlDbType.Variant;
            switch ((OleDbType)dbType)
            {
                case OleDbType.BigInt:
                    sqlType = SqlDbType.BigInt;
                    break;
                case OleDbType.Boolean:
                    sqlType = SqlDbType.Bit;
                    break;
                case OleDbType.Char:
                case OleDbType.VarChar:
                    // these guys are ambiguous - server sends over DBTYPE_STR in both cases
                    sqlType = (typeName == MetaTypeName.CHAR) ? SqlDbType.Char : SqlDbType.VarChar;
                    break;
                case OleDbType.Currency:
                    sqlType = (typeName == MetaTypeName.SMALLMONEY) ? SqlDbType.SmallMoney : SqlDbType.Money;
                    break;
                case OleDbType.Date:
                case OleDbType.DBTimeStamp:
                case OleDbType.Filetime:
                    switch (typeName)
                    {
                        case MetaTypeName.SMALLDATETIME:
                            sqlType = SqlDbType.SmallDateTime;
                            break;
                        case MetaTypeName.DATETIME2:
                            sqlType = SqlDbType.DateTime2;
                            break;
                        default:
                            sqlType = SqlDbType.DateTime;
                            break;
                    }
                    break;
                case OleDbType.Decimal:
                case OleDbType.Numeric:
                    sqlType = SqlDbType.Decimal;
                    break;
                case OleDbType.Double:
                    sqlType = SqlDbType.Float;
                    break;
                case OleDbType.Guid:
                    sqlType = SqlDbType.UniqueIdentifier;
                    break;
                case OleDbType.Integer:
                    sqlType = SqlDbType.Int;
                    break;
                case OleDbType.LongVarBinary:
                    sqlType = SqlDbType.Image;
                    break;
                case OleDbType.LongVarChar:
                    sqlType = SqlDbType.Text;
                    break;
                case OleDbType.LongVarWChar:
                    sqlType = SqlDbType.NText;
                    break;
                case OleDbType.Single:
                    sqlType = SqlDbType.Real;
                    break;
                case OleDbType.SmallInt:
                case OleDbType.UnsignedSmallInt:
                    sqlType = SqlDbType.SmallInt;
                    break;
                case OleDbType.TinyInt:
                case OleDbType.UnsignedTinyInt:
                    sqlType = SqlDbType.TinyInt;
                    break;
                case OleDbType.VarBinary:
                case OleDbType.Binary:
                    sqlType = (typeName == MetaTypeName.BINARY) ? SqlDbType.Binary : SqlDbType.VarBinary;
                    break;
                case OleDbType.Variant:
                    sqlType = SqlDbType.Variant;
                    break;
                case OleDbType.VarWChar:
                case OleDbType.WChar:
                case OleDbType.BSTR:
                    // these guys are ambiguous - server sends over DBTYPE_WSTR in both cases
                    // BSTR is always assumed to be NVARCHAR
                    sqlType = (typeName == MetaTypeName.NCHAR) ? SqlDbType.NChar : SqlDbType.NVarChar;
                    break;
                case OleDbType.DBDate: // Date
                    sqlType = SqlDbType.Date;
                    break;
                case (OleDbType)132: // Udt
                    sqlType = SqlDbType.Udt;
                    break;
                case (OleDbType)141: // Xml
                    sqlType = SqlDbType.Xml;
                    break;
                case (OleDbType)145: // Time
                    sqlType = SqlDbType.Time;
                    break;
                case (OleDbType)146: // DateTimeOffset
                    sqlType = SqlDbType.DateTimeOffset;
                    break;
                // TODO: Handle Structured types for derive parameters
                default:
                    break; // no direct mapping, just use SqlDbType.Variant;
            }

            return sqlType;
        }

        internal static MetaType GetSqlDataType(int tdsType, UInt32 userType, int length)
        {
            switch (tdsType)
            {
                case TdsEnums.SQLMONEYN:
                    return ((4 == length) ? MetaSmallMoney : MetaMoney);
                case TdsEnums.SQLDATETIMN:
                    return ((4 == length) ? MetaSmallDateTime : MetaDateTime);
                case TdsEnums.SQLINTN:
                    return ((4 <= length) ? ((4 == length) ? MetaInt : MetaBigInt) : ((2 == length) ? MetaSmallInt : MetaTinyInt));
                case TdsEnums.SQLFLTN:
                    return ((4 == length) ? MetaReal : MetaFloat);
                case TdsEnums.SQLTEXT:
                    return MetaText;
                case TdsEnums.SQLVARBINARY:
                    return MetaSmallVarBinary;
                case TdsEnums.SQLBIGVARBINARY:
                    return MetaVarBinary;

                case TdsEnums.SQLVARCHAR:           //goto TdsEnums.SQLBIGVARCHAR;
                case TdsEnums.SQLBIGVARCHAR:
                    return MetaVarChar;

                case TdsEnums.SQLBINARY:            //goto TdsEnums.SQLBIGBINARY;
                case TdsEnums.SQLBIGBINARY:
                    return ((TdsEnums.SQLTIMESTAMP == userType) ? MetaTimestamp : MetaBinary);

                case TdsEnums.SQLIMAGE:
                    return MetaImage;

                case TdsEnums.SQLCHAR:              //goto TdsEnums.SQLBIGCHAR;
                case TdsEnums.SQLBIGCHAR:
                    return MetaChar;

                case TdsEnums.SQLINT1:
                    return MetaTinyInt;

                case TdsEnums.SQLBIT:               //goto TdsEnums.SQLBITN;
                case TdsEnums.SQLBITN:
                    return MetaBit;

                case TdsEnums.SQLINT2:
                    return MetaSmallInt;
                case TdsEnums.SQLINT4:
                    return MetaInt;
                case TdsEnums.SQLINT8:
                    return MetaBigInt;
                case TdsEnums.SQLMONEY:
                    return MetaMoney;
                case TdsEnums.SQLDATETIME:
                    return MetaDateTime;
                case TdsEnums.SQLFLT8:
                    return MetaFloat;
                case TdsEnums.SQLFLT4:
                    return MetaReal;
                case TdsEnums.SQLMONEY4:
                    return MetaSmallMoney;
                case TdsEnums.SQLDATETIM4:
                    return MetaSmallDateTime;

                case TdsEnums.SQLDECIMALN:          //goto TdsEnums.SQLNUMERICN;
                case TdsEnums.SQLNUMERICN:
                    return MetaDecimal;

                case TdsEnums.SQLUNIQUEID:
                    return MetaUniqueId;
                case TdsEnums.SQLNCHAR:
                    return MetaNChar;
                case TdsEnums.SQLNVARCHAR:
                    return MetaNVarChar;
                case TdsEnums.SQLNTEXT:
                    return MetaNText;
                case TdsEnums.SQLVARIANT:
                    return MetaVariant;
                case TdsEnums.SQLUDT:
                    return MetaUdt;
                case TdsEnums.SQLXMLTYPE:
                    return MetaXml;
                case TdsEnums.SQLTABLE:
                    return MetaTable;
                case TdsEnums.SQLDATE:
                    return MetaDate;
                case TdsEnums.SQLTIME:
                    return MetaTime;
                case TdsEnums.SQLDATETIME2:
                    return MetaDateTime2;
                case TdsEnums.SQLDATETIMEOFFSET:
                    return MetaDateTimeOffset;

                case TdsEnums.SQLVOID:
                default:
                    Debug.Assert(false, "Unknown type " + tdsType.ToString(CultureInfo.InvariantCulture));
                    throw SQL.InvalidSqlDbType((SqlDbType)tdsType);
            }// case
        }

        internal static MetaType GetDefaultMetaType()
        {
            return MetaNVarChar;
        }

        // Converts an XmlReader into String
        internal static String GetStringFromXml(XmlReader xmlreader)
        {
            SqlXml sxml = new SqlXml(xmlreader);
            return sxml.Value;
        }

        private static readonly MetaType MetaBigInt = new MetaType
            (19, 255, 8, true, false, false, TdsEnums.SQLINT8, TdsEnums.SQLINTN, MetaTypeName.BIGINT, typeof(System.Int64), typeof(SqlInt64), SqlDbType.BigInt, DbType.Int64, 0);

        private static readonly MetaType MetaFloat = new MetaType
            (15, 255, 8, true, false, false, TdsEnums.SQLFLT8, TdsEnums.SQLFLTN, MetaTypeName.FLOAT, typeof(System.Double), typeof(SqlDouble), SqlDbType.Float, DbType.Double, 0);

        private static readonly MetaType MetaReal = new MetaType
            (7, 255, 4, true, false, false, TdsEnums.SQLFLT4, TdsEnums.SQLFLTN, MetaTypeName.REAL, typeof(System.Single), typeof(SqlSingle), SqlDbType.Real, DbType.Single, 0);

        // MetaBinary has two bytes of properties for binary and varbinary
        // 2 byte maxlen
        private static readonly MetaType MetaBinary = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLBIGBINARY, TdsEnums.SQLBIGBINARY, MetaTypeName.BINARY, typeof(System.Byte[]), typeof(SqlBinary), SqlDbType.Binary, DbType.Binary, 2);

        // syntatic sugar for the user...timestamps are 8-byte fixed length binary columns
        private static readonly MetaType MetaTimestamp = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLBIGBINARY, TdsEnums.SQLBIGBINARY, MetaTypeName.TIMESTAMP, typeof(System.Byte[]), typeof(SqlBinary), SqlDbType.Timestamp, DbType.Binary, 2);

        internal static readonly MetaType MetaVarBinary = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLBIGVARBINARY, TdsEnums.SQLBIGVARBINARY, MetaTypeName.VARBINARY, typeof(System.Byte[]), typeof(SqlBinary), SqlDbType.VarBinary, DbType.Binary, 2);

        internal static readonly MetaType MetaMaxVarBinary = new MetaType
            (255, 255, -1, false, true, true, TdsEnums.SQLBIGVARBINARY, TdsEnums.SQLBIGVARBINARY, MetaTypeName.VARBINARY, typeof(System.Byte[]), typeof(SqlBinary), SqlDbType.VarBinary, DbType.Binary, 2);

        // HACK!!!  We have an internal type for smallvarbinarys stored on TdsEnums.  We
        // store on TdsEnums instead of SqlDbType because we do not want to expose
        // this type to the user!
        private static readonly MetaType MetaSmallVarBinary = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLVARBINARY, TdsEnums.SQLBIGBINARY, ADP.StrEmpty, typeof(System.Byte[]), typeof(SqlBinary), TdsEnums.SmallVarBinary, DbType.Binary, 2);

        internal static readonly MetaType MetaImage = new MetaType
            (255, 255, -1, false, true, false, TdsEnums.SQLIMAGE, TdsEnums.SQLIMAGE, MetaTypeName.IMAGE, typeof(System.Byte[]), typeof(SqlBinary), SqlDbType.Image, DbType.Binary, 0);

        private static readonly MetaType MetaBit = new MetaType
            (255, 255, 1, true, false, false, TdsEnums.SQLBIT, TdsEnums.SQLBITN, MetaTypeName.BIT, typeof(System.Boolean), typeof(SqlBoolean), SqlDbType.Bit, DbType.Boolean, 0);

        private static readonly MetaType MetaTinyInt = new MetaType
            (3, 255, 1, true, false, false, TdsEnums.SQLINT1, TdsEnums.SQLINTN, MetaTypeName.TINYINT, typeof(System.Byte), typeof(SqlByte), SqlDbType.TinyInt, DbType.Byte, 0);

        private static readonly MetaType MetaSmallInt = new MetaType
            (5, 255, 2, true, false, false, TdsEnums.SQLINT2, TdsEnums.SQLINTN, MetaTypeName.SMALLINT, typeof(System.Int16), typeof(SqlInt16), SqlDbType.SmallInt, DbType.Int16, 0);

        private static readonly MetaType MetaInt = new MetaType
            (10, 255, 4, true, false, false, TdsEnums.SQLINT4, TdsEnums.SQLINTN, MetaTypeName.INT, typeof(System.Int32), typeof(SqlInt32), SqlDbType.Int, DbType.Int32, 0);

        // MetaVariant has seven bytes of properties for MetaChar and MetaVarChar
        // 5 byte tds collation
        // 2 byte maxlen
        private static readonly MetaType MetaChar = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLBIGCHAR, TdsEnums.SQLBIGCHAR, MetaTypeName.CHAR, typeof(System.String), typeof(SqlString), SqlDbType.Char, DbType.AnsiStringFixedLength, 7);

        private static readonly MetaType MetaVarChar = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLBIGVARCHAR, TdsEnums.SQLBIGVARCHAR, MetaTypeName.VARCHAR, typeof(System.String), typeof(SqlString), SqlDbType.VarChar, DbType.AnsiString, 7);

        internal static readonly MetaType MetaMaxVarChar = new MetaType
            (255, 255, -1, false, true, true, TdsEnums.SQLBIGVARCHAR, TdsEnums.SQLBIGVARCHAR, MetaTypeName.VARCHAR, typeof(System.String), typeof(SqlString), SqlDbType.VarChar, DbType.AnsiString, 7);

        internal static readonly MetaType MetaText = new MetaType
            (255, 255, -1, false, true, false, TdsEnums.SQLTEXT, TdsEnums.SQLTEXT, MetaTypeName.TEXT, typeof(System.String), typeof(SqlString), SqlDbType.Text, DbType.AnsiString, 0);

        // MetaVariant has seven bytes of properties for MetaNChar and MetaNVarChar
        // 5 byte tds collation
        // 2 byte maxlen
        private static readonly MetaType MetaNChar = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLNCHAR, TdsEnums.SQLNCHAR, MetaTypeName.NCHAR, typeof(System.String), typeof(SqlString), SqlDbType.NChar, DbType.StringFixedLength, 7);

        internal static readonly MetaType MetaNVarChar = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLNVARCHAR, TdsEnums.SQLNVARCHAR, MetaTypeName.NVARCHAR, typeof(System.String), typeof(SqlString), SqlDbType.NVarChar, DbType.String, 7);

        internal static readonly MetaType MetaMaxNVarChar = new MetaType
            (255, 255, -1, false, true, true, TdsEnums.SQLNVARCHAR, TdsEnums.SQLNVARCHAR, MetaTypeName.NVARCHAR, typeof(System.String), typeof(SqlString), SqlDbType.NVarChar, DbType.String, 7);

        internal static readonly MetaType MetaNText = new MetaType
            (255, 255, -1, false, true, false, TdsEnums.SQLNTEXT, TdsEnums.SQLNTEXT, MetaTypeName.NTEXT, typeof(System.String), typeof(SqlString), SqlDbType.NText, DbType.String, 7);

        // MetaVariant has two bytes of properties for numeric/decimal types
        // 1 byte precision
        // 1 byte scale
        internal static readonly MetaType MetaDecimal = new MetaType
            (38, 4, 17, true, false, false, TdsEnums.SQLNUMERICN, TdsEnums.SQLNUMERICN, MetaTypeName.DECIMAL, typeof(System.Decimal), typeof(SqlDecimal), SqlDbType.Decimal, DbType.Decimal, 2);

        internal static readonly MetaType MetaXml = new MetaType
            (255, 255, -1, false, true, true, TdsEnums.SQLXMLTYPE, TdsEnums.SQLXMLTYPE, MetaTypeName.XML, typeof(System.String), typeof(SqlXml), SqlDbType.Xml, DbType.Xml, 0);

        private static readonly MetaType MetaDateTime = new MetaType
            (23, 3, 8, true, false, false, TdsEnums.SQLDATETIME, TdsEnums.SQLDATETIMN, MetaTypeName.DATETIME, typeof(System.DateTime), typeof(SqlDateTime), SqlDbType.DateTime, DbType.DateTime, 0);

        private static readonly MetaType MetaSmallDateTime = new MetaType
            (16, 0, 4, true, false, false, TdsEnums.SQLDATETIM4, TdsEnums.SQLDATETIMN, MetaTypeName.SMALLDATETIME, typeof(System.DateTime), typeof(SqlDateTime), SqlDbType.SmallDateTime, DbType.DateTime, 0);

        private static readonly MetaType MetaMoney = new MetaType
            (19, 255, 8, true, false, false, TdsEnums.SQLMONEY, TdsEnums.SQLMONEYN, MetaTypeName.MONEY, typeof(System.Decimal), typeof(SqlMoney), SqlDbType.Money, DbType.Currency, 0);

        private static readonly MetaType MetaSmallMoney = new MetaType
            (10, 255, 4, true, false, false, TdsEnums.SQLMONEY4, TdsEnums.SQLMONEYN, MetaTypeName.SMALLMONEY, typeof(System.Decimal), typeof(SqlMoney), SqlDbType.SmallMoney, DbType.Currency, 0);

        private static readonly MetaType MetaUniqueId = new MetaType
            (255, 255, 16, true, false, false, TdsEnums.SQLUNIQUEID, TdsEnums.SQLUNIQUEID, MetaTypeName.ROWGUID, typeof(System.Guid), typeof(SqlGuid), SqlDbType.UniqueIdentifier, DbType.Guid, 0);

        private static readonly MetaType MetaVariant = new MetaType
            (255, 255, -1, true, false, false, TdsEnums.SQLVARIANT, TdsEnums.SQLVARIANT, MetaTypeName.VARIANT, typeof(System.Object), typeof(System.Object), SqlDbType.Variant, DbType.Object, 0);

        internal static readonly MetaType MetaUdt = new MetaType
            (255, 255, -1, false, false, true, TdsEnums.SQLUDT, TdsEnums.SQLUDT, MetaTypeName.UDT, typeof(System.Object), typeof(System.Object), SqlDbType.Udt, DbType.Object, 0);

        private static readonly MetaType MetaMaxUdt = new MetaType
            (255, 255, -1, false, true, true, TdsEnums.SQLUDT, TdsEnums.SQLUDT, MetaTypeName.UDT, typeof(System.Object), typeof(System.Object), SqlDbType.Udt, DbType.Object, 0);

        private static readonly MetaType MetaTable = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLTABLE, TdsEnums.SQLTABLE, MetaTypeName.TABLE, typeof(IEnumerable<DbDataRecord>), typeof(IEnumerable<DbDataRecord>), SqlDbType.Structured, DbType.Object, 0);

        // TODO: MetaSUDT is required for parameter.Add("", SqlDbType.Structured) to work.  We'll need to update it
        //    with real values when implementing structured UDTs.
        private static readonly MetaType MetaSUDT = new MetaType
            (255, 255, -1, false, false, false, TdsEnums.SQLVOID, TdsEnums.SQLVOID, "", typeof(SqlDataRecord), typeof(SqlDataRecord), SqlDbType.Structured, DbType.Object, 0);

        private static readonly MetaType MetaDate = new MetaType
            (255, 255, 3, true, false, false, TdsEnums.SQLDATE, TdsEnums.SQLDATE, MetaTypeName.DATE, typeof(System.DateTime), typeof(System.DateTime), SqlDbType.Date, DbType.Date, 0);

        internal static readonly MetaType MetaTime = new MetaType
            (255, 7, -1, false, false, false, TdsEnums.SQLTIME, TdsEnums.SQLTIME, MetaTypeName.TIME, typeof(System.TimeSpan), typeof(System.TimeSpan), SqlDbType.Time, DbType.Time, 1);

        private static readonly MetaType MetaDateTime2 = new MetaType
            (255, 7, -1, false, false, false, TdsEnums.SQLDATETIME2, TdsEnums.SQLDATETIME2, MetaTypeName.DATETIME2, typeof(System.DateTime), typeof(System.DateTime), SqlDbType.DateTime2, DbType.DateTime2, 1);

        internal static readonly MetaType MetaDateTimeOffset = new MetaType
            (255, 7, -1, false, false, false, TdsEnums.SQLDATETIMEOFFSET, TdsEnums.SQLDATETIMEOFFSET, MetaTypeName.DATETIMEOFFSET, typeof(System.DateTimeOffset), typeof(System.DateTimeOffset), SqlDbType.DateTimeOffset, DbType.DateTimeOffset, 1);

        public static TdsDateTime FromDateTime(DateTime dateTime, byte cb)
        {
            SqlDateTime sqlDateTime;
            TdsDateTime tdsDateTime = new TdsDateTime();

            Debug.Assert(cb == 8 || cb == 4, "Invalid date time size!");

            if (cb == 8)
            {
                sqlDateTime = new SqlDateTime(dateTime);
                tdsDateTime.time = sqlDateTime.TimeTicks;
            }
            else
            {
                // note that smalldatetime is days&minutes.
                // Adding 30 seconds ensures proper roundup if the seconds are >= 30
                // The AddSeconds function handles eventual carryover
                sqlDateTime = new SqlDateTime(dateTime.AddSeconds(30));
                tdsDateTime.time = sqlDateTime.TimeTicks / SqlDateTime.SQLTicksPerMinute;
            }
            tdsDateTime.days = sqlDateTime.DayTicks;
            return tdsDateTime;
        }


        public static DateTime ToDateTime(int sqlDays, int sqlTime, int length)
        {
            if (length == 4)
            {
                return new SqlDateTime(sqlDays, sqlTime * SqlDateTime.SQLTicksPerMinute).Value;
            }
            else
            {
                Debug.Assert(length == 8, "invalid length for DateTime");
                return new SqlDateTime(sqlDays, sqlTime).Value;
            }
        }

        internal static int GetTimeSizeFromScale(byte scale)
        {
            // Disable the assert here since we do not properly handle wrong Scale value on the parameter,
            // see VSTFDEVDIV 795578 for more details.
            // But, this assert is still valid when we receive Time/DateTime2/DateTimeOffset scale from server over TDS, 
            // so it is moved to TdsParser.CommonProcessMetaData.
            // For new scenarios, assert and/or validate the scale value before this call!
            // Debug.Assert(0 <= scale && scale <= 7);

            if (scale <= 2)
                return 3;

            if (scale <= 4)
                return 4;

            return 5;
        }

        //
        // please leave string sorted alphabetically
        // note that these names should only be used in the context of parameters.  We always send over BIG* and nullable types for SQL Server
        //
        private static class MetaTypeName
        {
            public const string BIGINT = "bigint";
            public const string BINARY = "binary";
            public const string BIT = "bit";
            public const string CHAR = "char";
            public const string DATETIME = "datetime";
            public const string DECIMAL = "decimal";
            public const string FLOAT = "float";
            public const string IMAGE = "image";
            public const string INT = "int";
            public const string MONEY = "money";
            public const string NCHAR = "nchar";
            public const string NTEXT = "ntext";
            public const string NVARCHAR = "nvarchar";
            public const string REAL = "real";
            public const string ROWGUID = "uniqueidentifier";
            public const string SMALLDATETIME = "smalldatetime";
            public const string SMALLINT = "smallint";
            public const string SMALLMONEY = "smallmoney";
            public const string TEXT = "text";
            public const string TIMESTAMP = "timestamp";
            public const string TINYINT = "tinyint";
            public const string UDT = "udt";
            public const string VARBINARY = "varbinary";
            public const string VARCHAR = "varchar";
            public const string VARIANT = "sql_variant";
            public const string XML = "xml";
            public const string TABLE = "table";
            public const string DATE = "date";
            public const string TIME = "time";
            public const string DATETIME2 = "datetime2";
            public const string DATETIMEOFFSET = "datetimeoffset";
        }
    }

    //
    // note: it is the client's responsibility to know what size date time he is working with
    //
    internal struct TdsDateTime
    {
        public int days;  // offset in days from 1/1/1900
        //     private UInt32 time;  // if smalldatetime, this is # of minutes since midnight
        // otherwise: # of 1/300th of a second since midnight
        public int time; // UNDONE, use UInt32 when available! (0716 compiler??)
    }

}

