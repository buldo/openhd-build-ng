internal static class SupportedPlatforms
{
    public static BuildPlatform RaspberryPi {get;} = new()
    {
        Name = nameof(RaspberryPi),
        NameStub = "pi",
        DebianReleaseName = "bullseye",
        Arch = ArchStrings.armhf,
        BuildDeps = 
        [
            "libpcap-dev"
        ],
        Mirrors = 
        [
            "deb [trusted=yes] http://raspbian.raspberrypi.org/raspbian/ bullseye main contrib non-free rpi",
            "deb [trusted=yes] http://archive.raspberrypi.org/debian/ bullseye main",
            "deb [trusted=yes] https://dl.cloudsmith.io/public/openhd/release/deb/raspbian bullseye main"
        ]
    };
}