using System;
using System.Linq;
using System.Reflection;
using StardewValley;
using Microsoft.Xna.Framework.Graphics;

class P {
  static void Main() {
    var t = typeof(StardewValley.Object);
    foreach (var m in t.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Where(m => m.Name.Contains("draw") || m.Name.Contains("Draw") || m.Name.Contains("getSourceRect") || m.Name.Contains("SourceRect") || m.Name.Contains("Texture") || m.Name.Contains("texture")).OrderBy(m => m.Name)) {
      Console.WriteLine(m);
    }
  }
}
