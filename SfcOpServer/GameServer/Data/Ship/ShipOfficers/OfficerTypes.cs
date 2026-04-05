#pragma warning disable IDE0130

namespace SfcOpServer
{
    /*
        This enumeration was obtained by setting the officers' names directly in the script using:
            tShipInfo::mSetOfficerName(	eOfficerTypes Officer, char *name); 

        and then by debugging the names reported by the mission
    */

    public enum OfficerTypes
    {
        kCaptainOfficer,
        kSecurityOfficer,
        kHelmOfficer,
        kCommOfficer,
        kScienceOfficer,
        kEngineeringOfficer,
        kWeaponsOfficer,

        kMaxOfficers
    };
}
