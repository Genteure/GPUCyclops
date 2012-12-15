using System;
using System.IO;

namespace Common
{
  public interface IUniSerializable
  {
    void Store(BinaryWriter writer);
    void Load(BinaryReader reader);
  }
}
