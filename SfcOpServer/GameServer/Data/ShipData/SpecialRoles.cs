#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum SpecialRoles
    {
        None = 1 << 0,

        C = 1 << 1,
        D = 1 << 2,
        K = 1 << 3,
        KM = 1 << 4,
        M = 1 << 5,
        NT = 1 << 6,
        S = 1 << 7,
        V = 1 << 8, // required for carriers?

        A = 1 << 9,
        E = 1 << 10,
        I = 1 << 11,
        L = 1 << 12,
        P = 1 << 13,
        Q = 1 << 14,
        R = 1 << 15,

        T = 1 << 16, // tournment ship?

        // helpers

        Ignored = R | T
    }
}
