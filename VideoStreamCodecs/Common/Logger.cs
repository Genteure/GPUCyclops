using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
  /// <summary>
  /// Provides basic logging infrastructure.
  /// </summary>
  public class Logger : IDisposable
  {
    bool _isDisposed = false;
    public bool IsDisposed
    {
      get { return _isDisposed; }
    }

    object _syncRoot = new object();
    public object SyncRoot
    {
      get { return _syncRoot; }
    }

    Logger[] _sinks = new Logger[] { };

    /// <summary>
    /// If an of these match a message, message will be dropped.
    /// </summary>
    public string[] StringFilters { get; set; }

    /// <summary>
    /// Use structs to prevent object construction performance hit.
    /// </summary>
    public struct LogEntry : IUniSerializable
    {
      /// <summary>
      /// An item can only have one of these values assigned, but some 
      /// external component may choose to do a combination for some purpose.
      /// </summary>
      public enum Types
      {
        Debug = 1,
        Info = 2,
        Warning = 4,
        Error = 8,
        Fatal = 16,

        Value = 32, // Value entries are different, as they should be 
        // displayed per value, not on the list. Message is valueName.
      }

      public Types Type { get; set; }
      public DateTime DateTime { get; set; }

      /// <summary>
      /// Name of the module (assembly) this entry originated from.
      /// </summary>
      public string ModuleName { get; set; }

      /// <summary>
      /// Name of the class this entry originated from.
      /// </summary>
      public string ClassName { get; set; }

      /// <summary>
      /// Name of the method that generated this entry.
      /// </summary>
      public string MethodName { get; set; }

      /// <summary>
      /// Entry message.
      /// </summary>
      public string Message { get; set; }

      /// <summary>
      /// In value entries, Message serves as Name.
      /// </summary>
      public string ValueName
      {
        get { return Message; }
      }

      /// <summary>
      /// Optional, applicable for Value Type messages only.
      /// </summary>
      public string ValueOptional { get; set; }

      /// <summary>
      /// Not serialized.
      /// </summary>
      public object[] Params { get; set; }

      public override string ToString()
      {
        if (Type == Types.Value)
        {
          return string.Format("[{0}, {1}] {2} - {3}", Type, DateTime.ToString("HH:mm:ss.fff"), Message, ValueOptional);
        }
        else if (Type == Types.Info)
        {// Info is a bit shorter.
          return string.Format("[{0}] {1}", DateTime.ToString("HH:mm:ss.fff"), Message);
        }
        else
        {// Default.
          return string.Format("[{0}, {1}] {2}", Type, DateTime.ToString("HH:mm:ss.fff"), Message);
        }
      }

      #region IUniSerializable Members

      void WriteSafe(System.IO.BinaryWriter writer, string mssage)
      {
        writer.Write(mssage != null ? mssage : string.Empty);
      }

      void Write(System.IO.BinaryWriter writer, DateTime dateTime)
      {
        // The resulting file time would represent a date and time before 12:00 midnight January 1, 1601 C.E. UTC.
        writer.Write(dateTime.ToFileTimeUtc());
      }

      public void Store(System.IO.BinaryWriter writer)
      {
        writer.Write((byte)this.Type);
        Write(writer, this.DateTime);
        WriteSafe(writer, this.Message);
      }

      DateTime ReadDateTime(System.IO.BinaryReader reader)
      {
        long value = reader.ReadInt64();
        return DateTime.FromFileTimeUtc(value);
      }

      public void Load(System.IO.BinaryReader reader)
      {
        this.Type = (Types)reader.ReadByte();
        this.DateTime = ReadDateTime(reader);
        this.Message = reader.ReadString();
      }

      #endregion
    }

    static Logger _instance = new Logger();
    public static Logger Instance
    {
      get { return _instance; }
    }

    /// <summary>
    /// This helps in case we wish to notify the user, on runtime, for errors, warnings etc.
    /// </summary>
    Logger.LogEntry? _lastErrorEntry = null;

    public delegate void EntryUpdate(Logger logger, LogEntry entry);
    public event EntryUpdate EntryLogged;

    public Logger()
    {
    }

    public Logger(Logger[] sinks)
    {
      foreach (var sink in sinks)
      {
        AddSink(sink);
      }
    }

    public virtual void Dispose()
    {
      _isDisposed = true;
    }

    protected void RaiseEntryLogged(LogEntry entry)
    {
      var del = this.EntryLogged;
      if (del != null)
        del(this, entry);
    }

    public Logger.LogEntry? PopLastErrorEntry()
    {
      Logger.LogEntry? entry;
      lock (_syncRoot)
      {
        entry = _lastErrorEntry;
        _lastErrorEntry = null;
      }

      return entry;
    }

    /// <summary>
    /// Add a child logger.
    /// </summary>
    public void AddSink(Logger sink)
    {
      lock (_syncRoot)
      {
        var newSinks = new Logger[_sinks.Length + 1];
        newSinks[newSinks.Length - 1] = sink;

        _sinks.CopyTo(newSinks, 0);
        _sinks = newSinks;
      }
    }

    /// <summary>
    /// Remove a child logger.
    /// </summary>
    public bool RemoveSink(Logger sink)
    {
      lock (_syncRoot)
      {
        List<Logger> newSinks = new List<Logger>(_sinks);
        if (newSinks.Remove(sink))
        {
          _sinks = newSinks.ToArray();
          return true;
        }
      }

      return false;
    }

    public void Log(LogEntry entry)
    {
      string[] filters = this.StringFilters;

      if (filters != null && filters.Length > 0)
      {
        foreach (string filter in filters)
        {
          if (string.IsNullOrEmpty(entry.Message) == false && entry.Message.Contains(filter))
          {// Abort log.
            return;
          }
        }
      }

      DoLog(entry);
      RaiseEntryLogged(entry);
    }

    public virtual void DoLog(LogEntry entry)
    {
      if (entry.Type == Logger.LogEntry.Types.Warning || entry.Type == Logger.LogEntry.Types.Error
         || entry.Type == Logger.LogEntry.Types.Fatal)
      {
        _lastErrorEntry = entry;
      }

      foreach (var sink in _sinks)
      {
        sink.Log(entry);
      }
    }

    public void Value(string valueName, string value)
    {
      Log(new LogEntry() { Message = valueName, DateTime = DateTime.Now, Type = LogEntry.Types.Value, ValueOptional = value });
    }

    public void Value(string moduleName, string className, string methodName, string valueName, string value)
    {
      Log(new LogEntry() { ModuleName = moduleName, ClassName = className, MethodName = methodName, Message = valueName, DateTime = DateTime.Now, Type = LogEntry.Types.Value, ValueOptional = value });
    }

    public void Debug(string message, params object[] objs)
    {
      Log(new LogEntry() { Message = message, DateTime = DateTime.Now, Type = LogEntry.Types.Debug, Params = objs });
    }

    public void Debug(string moduleName, string className, string methodName, string message, params object[] objs)
    {
      Log(new LogEntry() { ModuleName = moduleName, ClassName = className, MethodName = methodName, Message = message, DateTime = DateTime.Now, Type = LogEntry.Types.Debug, Params = objs });
    }

    public void Error(string message, params object[] objs)
    {
      Log(new LogEntry() { Message = message, DateTime = DateTime.Now, Type = LogEntry.Types.Error, Params = objs });
    }

    //public void Error(object emittingObject, string methodName, string message, params object[] objs)
    //{
    //   Type type = emittingObject.GetType();
    //   Error(type.Assembly.GetName().Name, type.Name, methodName, message, objs);
    //}

    //public void Error(Type emittingType, string methodName, string message, params object[] objs)
    //{
    //   Error(emittingType.Assembly.GetName().Name, emittingType.Name, methodName, message, objs);
    //}

    public void Error(object emitter, string methodName, string message)
    {
      var type = emitter.GetType();
#if SILVERLIGHT
			Log(new LogEntry()
			{
				ModuleName = "-",
				ClassName = type.Name,
				MethodName = methodName,
				Message = "[:" + emitter.GetHashCode() + "] " + message,
				DateTime = DateTime.Now,
				Type = LogEntry.Types.Error
			});
#else
      Log(new LogEntry()
      {
        ModuleName = type.Assembly.GetName().Name,
        ClassName = type.Name,
        MethodName = methodName,
        Message = "[:" + emitter.GetHashCode() + "] " + message,
        DateTime = DateTime.Now,
        Type = LogEntry.Types.Error
      });
#endif
    }

    public void Error(string moduleName, string className, string methodName, string message, params object[] objs)
    {
      Log(new LogEntry() { ModuleName = moduleName, ClassName = className, MethodName = methodName, Message = message, DateTime = DateTime.Now, Type = LogEntry.Types.Error, Params = objs });
    }

    public void Info(string message, params object[] objs)
    {
      Log(new LogEntry() { Message = message, DateTime = DateTime.Now, Type = LogEntry.Types.Info, Params = objs });
    }

    public void Info(string moduleName, string className, string methodName, string message, params object[] objs)
    {
      Log(new LogEntry() { ModuleName = moduleName, ClassName = className, MethodName = methodName, Message = message, DateTime = DateTime.Now, Type = LogEntry.Types.Info, Params = objs });
    }

    public void Warning(string message, params object[] objs)
    {
      Log(new LogEntry() { Message = message, DateTime = DateTime.Now, Type = LogEntry.Types.Warning, Params = objs });
    }

    public void Warning(string moduleName, string className, string methodName, string message, params object[] objs)
    {
      Log(new LogEntry() { ModuleName = moduleName, ClassName = className, MethodName = methodName, Message = message, DateTime = DateTime.Now, Type = LogEntry.Types.Warning, Params = objs });
    }

    public void Fatal(string message, params object[] objs)
    {
      Log(new LogEntry() { Message = message, DateTime = DateTime.Now, Type = LogEntry.Types.Fatal, Params = objs });
    }

    public void Fatal(string moduleName, string className, string methodName, string message, params object[] objs)
    {
      Log(new LogEntry() { ModuleName = moduleName, ClassName = className, MethodName = methodName, Message = message, DateTime = DateTime.Now, Type = LogEntry.Types.Fatal, Params = objs });
    }
  }
}

