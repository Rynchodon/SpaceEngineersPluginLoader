Intro
=====
Space Engineers Plugin Loader is an application for downloading plugins for Space Engineers from GitHub and loading them into the game. SEPL automatically updates plugins and ensures that the correct version of each plugin is loaded for the version of Space Engineers currently being played.


Install
=======
Download SEPL_win64_installer.zip, open it, and run SetupSEPL.msi
A shortcut to Plugin Manager will be placed on your desktop and in the start menu.


Config
======
Launch Plugin Manager.

Plugins are defined in the grid portion at the top:
	The blank row at the bottom is for adding new entries; it cannot be deleted and will not be saved.
	Enabled: The box must be ticked for the plugin to be loaded into Space Engineers.
	Author: The GitHub author of the plugin, this may be different from the name the author uses on steam.
	Repository: The name of the plugin on GitHub.
	Pre-Release: Enable this only if you are the author or are instructed to do so by the author. Enables downloading of pre-release versions of the plugin.
	Status:
		Checkmark: The Author and Repository are correct and the plugin can be downloaded from GitHub. The plugin's configuration will be saved.
		Magnifying Glass: Checking GitHub to see if the plugin can be located. The plugin's configuration will be saved.
		Warning Symbol: Either you are not connected to the internet or no repository exists for the plugin; check your spelling. The plugin's configuration will be saved.
		X: The Author or Repository field is empty. The plugin's configuration cannot be saved.
		Blank: Author and Repository have not been filled in. The plugin's configuration cannot be saved.
	Delete: Remove a plugin from the list.

Buttons:
	Save: Saves the configuration of all plugins.
	Help: Display help text.
	Launch SE and SEPL: Launch Space Engineers and load all enabled plugins.
	Launch DS and SEPL: Launch Space Engineers Dedicated Server and load all enabled plugins.

(Modder Only) Path to git: Only for publishing plugins, not needed to configure plugins.


Launch Without Plugin Manager
=============================
Follow these steps to have SEPL automatically load plugins every time Space Engineers is launched.
In steam library, right click on Space Engineers
Left click on properties
Left click on "SET LAUNCH OPTIONS..."
If the text box is not empty, leave a space
Add "-plugin PluginLoader.dll" without the quotes
Left click "OK"
Left click "CLOSE"


Plugin Authors
==============
Plugins can be added to SEPL locally, so they can be tested, and they can be published to GitHub.
SEPL decides which release to download from its tag name. For this reason, tag name cannot be specified while publishing a plugin through SEPL.

Plugins are configured through the use of a json or an xml file which is then passed to PluginManager.exe.
This guide assumes you are familiar with either json or xml files.
Download one of these templates:
	https://raw.githubusercontent.com/Rynchodon/SpaceEngineersPluginLoader/master/template.json
	https://raw.githubusercontent.com/Rynchodon/SpaceEngineersPluginLoader/master/template.xml
and put it in your plugin's directory; this will be your build file.

In the build file:
	Any optional elements can be null.

	author: The author of the GitHub repository.
	files: Array of files to be included in the plugin.
		requires(Optional): Array of files in this plugin that must be loaded before this file. Only file name is required.
		source: Path to file. It can be absolute, relative to the current working directory, or relative to the directory of the build file.
		targetFolder(Optional): The target sub-folder for the file.
	publish: Upload the plugin to GitHub.
	release: Information for GitHub release
		body(Optional): The body of the release. Use "\\n" for newline.
		body_lines(Optional): Strings that will be appended to body as lines.
		draft: Mark the release as incomplete.
		name(Optional): The name of the release.
		prerelease: Mark the release as not production ready.
		target_commitish(Optional): Commit to target with the release. Defaults to head of master branch.
	repository: GitHub repository for the plugin.
	requires(Optional): Array of plugins that must be loaded before this plugin
		author: GitHub author of required plugin.
		repository: GitHub repository of required plugin.
	version: The mod version and the version of SE it was compiled against.
		If Major, Minor, Build, and Revision are all zero, SEPL will get the version number from assemblies included in the plugin.
		If SeVersion is zero, SEPL will get the SE version from the currently downloaded copy of Space Engineers. If SeVersion is less than zero, SEPL will consider the plugin compatible with every version of Space Engineers.
	zipFileName(Optional): Name of the assets' zip file. Defaults to plugin's repository's name.

You will need a GitHub OAuth Token to publish your plugin.
OAuth Token can be specified in the command line or by the environment variable oAuthToken.

The options that can be specified on the command line are: author, oAuthToken, publish, repository, zipFileName, body, draft, name, prerelease, target_commitish, Build, Major, Minor, Revision, and SeVersion.
	Command line options override the options from the build file.
	Options are specified "name=value"

To add/publish your plugin execute PluginManger.exe with the path to your build file as an argument as well as any additional options.

For example:
	PluginManger.exe build.json
	PluginManger.exe build.xml publish=true oAuthToken=12345

If path to git was set with Plugin Manger's GUI, SEPL will warn if you attempt to publish from the wrong branch, a dirty branch, or one with unsynced commits.

If you take down a release, users will have their plugins rolled back.


Release Versions
================
SEPL looks for the release with the highest Space Engineers version that is not newer than the version currently being played. If there is more than one release with the same Space Engineers version, the one with the highest plugin version will be chosen.
Consider a plugin with the following release versions:
	version 0.9, SE version 1.175
	version 1.0, SE version 1.170
	version 1.1, SE version 1.180
	version 1.2, SE version 1.180
If the current SE version is less than 1.170, no version will be downloaded.
If the current SE version is 1.170, version 1.0 will be downloaded.
If the current SE version is 1.175, version 0.9 will be downloaded.
If the current SE version is 1.180 or higher, version 1.2 will be downloaded.

If the same plugin also had a release:
	version 1.3, SE version any (-1 in build file)
version 1.3 would be downloaded for SE versions less than 1.170 but no others.
