<p align="center">  <br>
  <img height="74" src="https://raw.githubusercontent.com/krisdb2009/documentation/master/images/supersweet.png">
  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <img height="80" src="https://raw.githubusercontent.com/belowaverage-org/SuperGrate/master/SuperGrate/Images/supergrate.ico">
  <br><br><p>
<h1 align="center">
  <img src="https://belowaverage.visualstudio.com/SuperGrate/_apis/build/status/belowaverage-org.SuperGrate?branchName=master"><br><br>
</h1>
<h3 align="center">
  <a href="https://github.com/belowaverage-org/SuperGrate/releases">Download</a> | <a href="https://belowaverage.org/software/supergrate">Website</a> | <a href="https://github.com/belowaverage-org/SuperGrate/wiki">Documentation</a> | <a href="https://github.com/belowaverage-org/SuperGrate/issues">Issues</a>
</h3>

<h1></h1>
  <img src="https://raw.githubusercontent.com/krisdb2009/documentation/master/supergrate/promo.png">
  <h1></h1>


  
    <b>Super Grate</b> is a tool part of the Super Suite that can perform remote execution of Microsoft's USMT (User State Migration Tool) on any domain joined PC, or local execution on any non-joined PC.
  </p>
  <h2>Getting Started</h2>
  <p>Getting started with Super Grate is super easy! Simply <a href="https://github.com/belowaverage-org/SuperGrate/releases">download</a> the installer, run the setup wizard, and begin migrating user profiles! </p>
  <p>There are no prerequisites, Super Grate will automatically download any nessesary components from this repository.</p>
  <h2>Features</h2>
  <ul>
    <li>Simple GUI</li>
    <li>Configurable</li>
    <li>Works on Domain Joined and Non-Joined PCs</li>
    <li>Unified Backup Store</li>
    <li>Works remotely over a network</li>
  </ul>
  <h2>Configuration</h2>

```xml
<?xml version="1.0" encoding="utf-8"?>
<SuperGrate>
  <!--The UNC or Direct path to the USMT directory. (E.g: .\USMT\X64)-->
  <USMTPathX64>.\USMT\X64</USMTPathX64>
  <USMTPathX86>.\USMT\X86</USMTPathX86>
  <!--Local path on source computer where Super Grate will run USMT from. (E.g: C:\SuperGrate)-->
  <SuperGratePayloadPath>C:\SuperGrate</SuperGratePayloadPath>
  <!--The UNC or Direct path to the USMT Migration Store (E.g: \\ba-share\s$ or .\STORE)-->
  <MigrationStorePath>.\STORE</MigrationStorePath>
  <!--ScanState.exe & LoadState.exe CLI Parameters. See: https://docs.microsoft.com/en-us/windows/deployment/usmt/usmt-command-line-syntax -->
  <ScanStateParameters>/config:Config_SettingsOnly.xml /i:MigUser.xml /c /r:3 /o</ScanStateParameters>
  <LoadStateParameters>/config:Config_SettingsOnly.xml /i:MigUser.xml /c /r:3 /lac /lae</LoadStateParameters>
  <!--Delete the user from the migration store after a restore? (store to destination)-->
  <AutoDeleteFromStore>false</AutoDeleteFromStore>
  <!--Delete the user from the source computer after a backup? (source to store)-->
  <AutoDeleteFromSource>false</AutoDeleteFromSource>
  <!--Prevent NT AUTHORITY & NT SERVICE accounts from being listed?-->
  <HideBuiltInAccounts>true</HideBuiltInAccounts>
  <!--Prevent unknown accounts from being listed?-->
  <HideUnknownSIDs>false</HideUnknownSIDs>
  <!--Write log to disk on exit. (Leave blank to disable) (E.g: \\ba-share\s$\Logs or .\Logs)-->
  <DumpLogHereOnExit></DumpLogHereOnExit>
  <!--List of columns to display for the Source or Store users.-->
  <ULSourceColumns>0,3,9</ULSourceColumns>
  <ULStoreColumns>0,1,5,6,4</ULStoreColumns>
</SuperGrate>
```
