using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Your_Judge.Classes
{
    public class ProgrammingLanguage
    {
        public override string ToString()
        {
            return Alias;
        }
        public ProgrammingLanguage(string Name, string Alias, string FileExtension, string DefaultCode)
        {
            _Name = Name;
            _Alias = Alias;
            _FileExtension = FileExtension;
            _DefaultCode = DefaultCode;
        }
        private string _Name { get; set; }
        public string Name { get => _Name; }
        private string _Alias { get; set; }
        public string Alias { get => _Alias; }
        private string _DefaultCode { get; set; }
        public string DefaultCode { get => _DefaultCode; }
        private string _FileExtension{ get; set; }
        public string FileExtension { get => _FileExtension; }
        public static List<ProgrammingLanguage> Available = new List<ProgrammingLanguage>()
        {
            new("Java", "java", ".java",

@"import java.util.Scanner;

public class <#? defaultclass ?#>
{  
    public static void main(String args[])
    {
        Scanner sc = new Scanner(System.in);
        System.out.println(""Unpar"");
    }
}"),

            new("C++", "cpp", ".cpp",
@"#include <iostream>

int main() {
    std::cout << ""Unpar"";

    return 0;
}"),

            new("C#", "csharp", ".cs",
@"using System;

public class <#? defaultclass ?#>
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""Unpar"");
    }
}"),

            new("Python", "python", ".py",
@"print(""Unpar"")"),

            new("JavaScript", "javascript", ".js",
@"console.log(""Unpar"")")
        };
    }
}
