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
            "libpcap-dev",
            "libusb-1.0-0-dev",
            "libpcap-dev", 
            "libsodium-dev", 
            "libnl-3-dev",
            "libnl-genl-3-dev",
            "libnl-route-3-dev",
            "libsdl2-dev",
            "libgstreamer-plugins-base1.0-dev",
            "libgstreamer1.0-dev",
            "libv4l-dev",
            "libcamera-openhd",
            "libgnutls30"
        ],
        Mirrors = 
        [
            "deb [trusted=yes] http://raspbian.raspberrypi.org/raspbian/ bullseye main contrib non-free rpi",
            "deb [trusted=yes] http://archive.raspberrypi.org/debian/ bullseye main",
            "deb [trusted=yes] https://dl.cloudsmith.io/public/openhd/release/deb/raspbian bullseye main"
        ]
    };
}