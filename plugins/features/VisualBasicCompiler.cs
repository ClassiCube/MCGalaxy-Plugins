//reference System.dll
using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using MCGalaxy.Modules.Compiling;
using MCGalaxy;

public sealed class VisualBasicPlugin : Plugin
{
    public override string creator { get { return "Not UnknownShadow200"; } }
    public override string MCGalaxy_Version { get { return "1.9.5.2"; } }
    public override string name { get { return "VisualBasicPlugin"; } }

    ICompiler compiler = new VBCompiler();
    public override void Load(bool startup) {
        ICompiler.Compilers.Add(compiler);
    }
    
    public override void Unload(bool shutdown) {
        ICompiler.Compilers.Remove(compiler);
    }
}

abstract class CodeDomCompiler : ICompiler
{
    public static CompilerParameters PrepareInput(string[] srcPaths, string dstPath, List<string> referenced) {
        CompilerParameters args = new CompilerParameters();
        args.GenerateExecutable      = false;
        args.IncludeDebugInformation = true;
        args.OutputAssembly          = dstPath;

        foreach (string assembly in referenced)
        {
            args.ReferencedAssemblies.Add(assembly);
        }
        return args;
    }

    public static CodeDomProvider InitCompiler(ICompiler c, string language) {
        var compiler = CodeDomProvider.CreateProvider(language);
        if (compiler != null) return compiler;
        
        Logger.Log(LogType.Warning,
                   "WARNING: {0} compiler is missing, you will be unable to compile {1} files.",
                   c.FullName, c.FileExtension);
        // TODO: Should we log "You must have .net developer tools. (You need a visual studio)" ?
        return null;
    }

    public static ICompilerErrors Compile(CompilerParameters args, string[] srcPaths, CodeDomProvider compiler) {
        CompilerResults results = compiler.CompileAssemblyFromFile(args, srcPaths);
        ICompilerErrors errors  = new ICompilerErrors();

        foreach (CompilerError error in results.Errors)
        {
            ICompilerError ce = new ICompilerError();
            ce.Line        = error.Line;
            ce.Column      = error.Column;
            ce.ErrorNumber = error.ErrorNumber;
            ce.ErrorText   = error.ErrorText;
            ce.IsWarning   = error.IsWarning;
            ce.FileName    = error.FileName;

            errors.Add(ce);
        }
        return errors;
    }
}

sealed class VBCompiler : CodeDomCompiler
{
    public override string FileExtension { get { return ".vb"; } }
    public override string ShortName	 { get { return "VB"; } }
    public override string FullName	     { get { return "Visual Basic"; } }
    CodeDomProvider compiler;

    protected override ICompilerErrors DoCompile(string[] srcPaths, string dstPath) {
        List<string> referenced = ProcessInput(srcPaths, "'");
        CompilerParameters args = PrepareInput(srcPaths, dstPath, referenced);

        if (compiler == null) compiler = InitCompiler(this, "VisualBasic");
        if (compiler == null) throw new NotSupportedException("VB compiling not currently functional");
        
        return Compile(args, srcPaths, compiler);
    }
    
    
    public override string CommandSkeleton {
        get {
            return @"'\tAuto-generated command skeleton class.
'\tUse this as a basis for custom MCGalaxy commands.
'\tNaming should be kept consistent. (e.g. /update command should have a class name of 'CmdUpdate' and a filename of 'CmdUpdate.vb')
' As a note, MCGalaxy is designed for .NET 4.0.

' To reference other assemblies, put a ""'reference [assembly filename]"" at the top of the file
'   e.g. to reference the System.Data assembly, put ""'reference System.Data.dll""

' Add any other Imports statements you need after this
Imports System
Imports MCGalaxy

Public Class Cmd{0}
\tInherits Command

\t' The command's name (what you put after a slash to use this command)
\tPublic Overrides ReadOnly Property name() As String
\t\tGet
\t\t\tReturn ""{0}""
\t\tEnd Get
\tEnd Property

\t' Command's shortcut, can be left blank (e.g. ""/Copy"" has a shortcut of ""c"")
\tPublic Overrides ReadOnly Property shortcut() As String
\t\tGet
\t\t\tReturn """"
\t\tEnd Get
\tEnd Property

\t' Which submenu this command displays in under /Help
\tPublic Overrides ReadOnly Property type() As String
\t\tGet
\t\t\tReturn ""other""
\t\tEnd Get
\t End Property

\t' Whether or not this command can be used in a museum. Block/map altering commands should return False to avoid errors.
\tPublic Overrides ReadOnly Property museumUsable() As Boolean
\t\tGet
\t\t\tReturn True
\t\tEnd Get
\tEnd Property

\t' The default rank required to use this command. Valid values are:
\t'   LevelPermission.Guest, LevelPermission.Builder, LevelPermission.AdvBuilder,
\t'   LevelPermission.Operator, LevelPermission.Admin, LevelPermission.Owner
\tPublic Overrides ReadOnly Property defaultRank() As LevelPermission
\t\tGet
\t\t\tReturn LevelPermission.Guest
\t\tEnd Get
\tEnd Property

\t' This is for when a player executes this command by doing /{0}
\t'   p is the player object for the player executing the command.
\t'   message is the arguments given to the command. (e.g. for '/{0} this', message is ""this"")
\tPublic Overrides Sub Use(p As Player, message As String)
\t\tp.Message(""Hello World!"")
\tEnd Sub

\t' This is for when a player does /Help {0}
\tPublic Overrides Sub Help(p As Player)
\t\tp.Message(""/{0} - Does stuff. Example command."")
\tEnd Sub
End Class";
        }
    }

    public override string PluginSkeleton {
        get {
            return @"' This is an example plugin source!
Imports System

Namespace MCGalaxy
\tPublic Class {0}
\t\tInherits Plugin

\t\tPublic Overrides ReadOnly Property name() As String
\t\t\tGet
\t\t\t\tReturn ""{0}""
\t\t\tEnd Get
\t\t End Property
\t\tPublic Overrides ReadOnly Property MCGalaxy_Version() As String
\t\t\tGet
\t\t\t\tReturn ""{2}""
\t\t\tEnd Get
\t\t End Property
\t\tPublic Overrides ReadOnly Property welcome() As String
\t\t\tGet
\t\t\t\tReturn ""Loaded Message!""
\t\t\tEnd Get
\t\t End Property
\t\tPublic Overrides ReadOnly Property creator() As String
\t\t\tGet
\t\t\t\tReturn ""{1}""
\t\t\tEnd Get
\t\t End Property

\t\tPublic Overrides Sub Load(startup As Boolean)
\t\t\t' LOAD YOUR PLUGIN WITH EVENTS OR OTHER THINGS!
\t\tEnd Sub
						
\t\tPublic Overrides Sub Unload(shutdown As Boolean)
\t\t\t' UNLOAD YOUR PLUGIN BY SAVING FILES OR DISPOSING OBJECTS!
\t\tEnd Sub
						
\t\tPublic Overrides Sub Help(p As Player)
\t\t\t' HELP INFO!
\t\tEnd Sub
\tEnd Class
End Namespace";
        }
    }
}
