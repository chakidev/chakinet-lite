try
{
    new AddLicenseFiles.AddLicenseFiles().Run(args[0], args[1]);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}