using CoachOS.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;

// Separate process per mode: no key cache can make a lost-key restore pass.
const string Fictional = "fictional-local-token-not-a-Mollie-credential";
string mode = args[0];
Directory.CreateDirectory(args[1]);
IDataProtectionProvider provider = DataProtectionProvider.Create(new DirectoryInfo(args[1]),
    configuration => configuration.SetApplicationName(args.Length > 3 ? args[3] : "CoachOS"));
DataProtectionTokenProtector protector = new(provider);
if (mode == "protect")
{
    File.WriteAllText(args[2], protector.Protect(Fictional));
    Console.WriteLine("Fictional payload protected; no key/token contents logged.");
}
else
{
    try
    {
        if (protector.Unprotect(File.ReadAllText(args[2])) != Fictional) return 2;
        Console.WriteLine("Fictional payload restored in a fresh process.");
    }
    catch (System.Security.Cryptography.CryptographicException) { return 3; }
}
return 0;
