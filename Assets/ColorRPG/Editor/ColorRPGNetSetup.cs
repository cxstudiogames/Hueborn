// =====================================================================
//  ColorRPGNetSetup.cs - unelte pentru multiplayer:
//   * ColorRPG > Online > Install Multiplayer Packages: instaleaza Netcode for
//     GameObjects + Multiplayer Services (Relay/Sessions) la versiunea recomandata.
//   * Promo/mpinstall.flag -> acelasi lucru automat (rezultat in Promo/Logs/mp_install.txt)
//   * Promo/apidump.flag   -> scrie API-ul public al pachetelor in Promo/Logs/api_dump.txt
// =====================================================================
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGNetSetup
    {
        static string LogDir { get { return Path.Combine(PromoDirector.Folder, "Logs"); } }
        static string InstallFlag { get { return Path.Combine(PromoDirector.Folder, "mpinstall.flag"); } }
        static string DumpFlag { get { return Path.Combine(PromoDirector.Folder, "apidump.flag"); } }
        static AddAndRemoveRequest req;

        static ColorRPGNetSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (File.Exists(InstallFlag))
                {
                    string[] pk = null;
                    try { pk = File.ReadAllText(InstallFlag).Split(new[] { '\n', '\r', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries); File.Delete(InstallFlag); } catch (Exception) { }
                    Install(pk != null && pk.Length > 0 && pk[0].StartsWith("com.") ? pk : null);
                    return;
                }
                if (File.Exists(DumpFlag)) { try { File.Delete(DumpFlag); } catch (Exception) { } Dump(); }
            };
        }

        [MenuItem("ColorRPG/Online/Install Multiplayer Packages", false, 40)]
        public static void InstallMenu() { Install(null); }

        public static void Install(string[] packages)
        {
            if (req != null && !req.IsCompleted) return;
            req = Client.AddAndRemove(packages ?? new[] { "com.unity.netcode.gameobjects", "com.unity.services.multiplayer" }, null);
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            Debug.Log("[ColorRPG] Installing multiplayer packages...");
        }

        static void Poll()
        {
            if (req == null || !req.IsCompleted) return;
            EditorApplication.update -= Poll;
            var sb = new StringBuilder("time: " + DateTime.Now + "\nstatus: " + req.Status + "\n");
            if (req.Status == StatusCode.Success)
                foreach (var p in req.Result) if (p.name.Contains("netcode") || p.name.Contains("vivox") || p.name.Contains("multiplayer") || p.name.Contains("transport") || p.name.Contains("services"))
                        sb.AppendLine(p.name + " " + p.version);
            if (req.Error != null) sb.AppendLine("error: " + req.Error.message);
            Directory.CreateDirectory(LogDir);
            File.WriteAllText(Path.Combine(LogDir, "mp_install.txt"), sb.ToString());
            Debug.Log("[ColorRPG] Multiplayer packages: " + req.Status);
        }

        static readonly string[] NetcodeTypes = {
            "NetworkManager", "CustomMessagingManager", "FastBufferWriter", "FastBufferReader", "NetworkConfig",
            "UnityTransport", "NetworkDelivery", "ConnectionEventData", "ConnectionEvent", "NetworkTransport" };

        [MenuItem("ColorRPG/Online/Dump Multiplayer API", false, 41)]
        public static void Dump()
        {
            var sb = new StringBuilder();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string an = asm.GetName().Name;
                bool mp = an.StartsWith("Unity.Services.Multiplayer") || an.StartsWith("Unity.Services.Vivox");
                bool nc = an == "Unity.Netcode.Runtime" || an == "Unity.Networking.Transport";
                if (!mp && !nc) continue;
                sb.AppendLine("##### " + an);
                Type[] types;
                try { types = asm.GetExportedTypes(); } catch (Exception e) { sb.AppendLine("ERR " + e.Message); continue; }
                foreach (var t in types.OrderBy(x => x.FullName))
                {
                    if (nc && !NetcodeTypes.Contains(t.Name)) continue;
                    if (mp && t.Namespace != null && (t.Namespace.Contains("Internal") || t.Namespace.Contains("Editor") || t.Namespace.Contains("Matchmaker") || t.Namespace.Contains("Qos") || t.Namespace.Contains("Multiplay") && !t.Namespace.EndsWith("Multiplayer"))) continue;
                    sb.AppendLine("== " + Kind(t) + " " + t.FullName + (t.BaseType != null && t.BaseType != typeof(object) ? " : " + t.BaseType.Name : "")
                        + (t.GetInterfaces().Length > 0 ? " [" + string.Join(",", t.GetInterfaces().Select(i => i.Name)) + "]" : ""));
                    if (t.IsEnum) { sb.AppendLine("   " + string.Join(", ", Enum.GetNames(t))); continue; }
                    var f = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                    foreach (var p in t.GetProperties(f)) sb.AppendLine("   P " + N(p.PropertyType) + " " + p.Name + (p.CanWrite ? " {get;set;}" : ""));
                    foreach (var e in t.GetEvents(f)) sb.AppendLine("   E " + N(e.EventHandlerType) + " " + e.Name);
                    foreach (var fi in t.GetFields(f)) sb.AppendLine("   F " + (fi.IsStatic ? "static " : "") + N(fi.FieldType) + " " + fi.Name);
                    foreach (var c in t.GetConstructors()) sb.AppendLine("   C (" + Pars(c) + ")");
                    foreach (var m in t.GetMethods(f).Where(m => !m.IsSpecialName))
                        sb.AppendLine("   M " + (m.IsStatic ? "static " : "") + N(m.ReturnType) + " " + m.Name
                            + (m.IsGenericMethod ? "<" + string.Join(",", m.GetGenericArguments().Select(N)) + ">" : "") + "(" + Pars(m) + ")");
                }
            }
            Directory.CreateDirectory(LogDir);
            File.WriteAllText(Path.Combine(LogDir, "api_dump.txt"), sb.ToString());
            File.WriteAllText(Path.Combine(LogDir, "api_dump_vivox.txt"), DumpOnly("Unity.Services.Vivox"));
            Debug.Log("[ColorRPG] API dump written (" + sb.Length + " chars)");
        }

        static string DumpOnly(string prefix)
        {
            var sb = new StringBuilder();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!asm.GetName().Name.StartsWith(prefix)) continue;
                sb.AppendLine("##### " + asm.GetName().Name);
                Type[] types;
                try { types = asm.GetExportedTypes(); } catch (Exception e) { sb.AppendLine("ERR " + e.Message); continue; }
                foreach (var t in types.OrderBy(x => x.FullName))
                {
                    if (t.Namespace != null && (t.Namespace.Contains("Internal") || t.Namespace.Contains("Editor"))) continue;
                    sb.AppendLine("== " + Kind(t) + " " + t.FullName);
                    if (t.IsEnum) { sb.AppendLine("   " + string.Join(", ", Enum.GetNames(t))); continue; }
                    var f = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                    foreach (var p in t.GetProperties(f)) sb.AppendLine("   P " + N(p.PropertyType) + " " + p.Name);
                    foreach (var e in t.GetEvents(f)) sb.AppendLine("   E " + N(e.EventHandlerType) + " " + e.Name);
                    foreach (var m in t.GetMethods(f).Where(m => !m.IsSpecialName))
                        sb.AppendLine("   M " + (m.IsStatic ? "static " : "") + N(m.ReturnType) + " " + m.Name + "(" + Pars(m) + ")");
                    foreach (var c in t.GetConstructors()) sb.AppendLine("   C (" + Pars(c) + ")");
                }
            }
            return sb.ToString();
        }

        static string Kind(Type t) { return t.IsInterface ? "interface" : t.IsEnum ? "enum" : t.IsValueType ? "struct" : (t.IsAbstract && t.IsSealed) ? "static class" : "class"; }
        static string Pars(MethodBase m) { return string.Join(", ", m.GetParameters().Select(p => N(p.ParameterType) + " " + p.Name + (p.IsOptional ? "=" + (p.DefaultValue ?? "null") : ""))); }
        static string N(Type t)
        {
            if (t == null) return "?";
            if (t.IsGenericType)
            {
                string b = t.Name; int i = b.IndexOf('`'); if (i > 0) b = b.Substring(0, i);
                return b + "<" + string.Join(",", t.GetGenericArguments().Select(N)) + ">";
            }
            return t.Name;
        }
    }
}
