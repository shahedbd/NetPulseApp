namespace NetPulseApp.Service.Network
{
    /// <summary>
    /// Maps DOT11_AUTH_ALGORITHM / DOT11_CIPHER_ALGORITHM values to the same
    /// short labels the Saved WiFi page shows ("WPA2-Personal", "Open", …).
    /// </summary>
    public static class WifiSecurityMapper
    {
        // DOT11_AUTH_ALGORITHM
        private const int AUTH_OPEN = 1;
        private const int AUTH_SHARED_KEY = 2;
        private const int AUTH_WPA = 3;
        private const int AUTH_WPA_PSK = 4;
        private const int AUTH_RSNA = 6;
        private const int AUTH_RSNA_PSK = 7;
        private const int AUTH_WPA3_ENT_192 = 8;
        private const int AUTH_WPA3_SAE = 9;
        private const int AUTH_OWE = 10;
        private const int AUTH_WPA3_ENT = 11;

        // DOT11_CIPHER_ALGORITHM
        private const int CIPHER_NONE = 0;
        private const int CIPHER_WEP40 = 1;
        private const int CIPHER_WEP104 = 5;
        private const int CIPHER_WEP = 0x101;

        public static string Label(int auth, int cipher, bool securityEnabled)
        {
            if (IsWep(auth, cipher)) return "WEP (insecure)";
            if (IsOpen(auth, cipher, securityEnabled)) return "Open";

            return auth switch
            {
                AUTH_WPA => "WPA-Enterprise",
                AUTH_WPA_PSK => "WPA-Personal",
                AUTH_RSNA => "WPA2-Enterprise",
                AUTH_RSNA_PSK => "WPA2-Personal",
                AUTH_WPA3_SAE => "WPA3-Personal",
                AUTH_WPA3_ENT or AUTH_WPA3_ENT_192 => "WPA3-Enterprise",
                AUTH_OWE => "OWE",
                _ => "Secured"
            };
        }

        public static bool IsOpen(int auth, int cipher, bool securityEnabled)
            => !securityEnabled || (auth == AUTH_OPEN && cipher == CIPHER_NONE);

        public static bool IsWep(int auth, int cipher)
            => auth == AUTH_SHARED_KEY
               || cipher is CIPHER_WEP40 or CIPHER_WEP104 or CIPHER_WEP;
    }
}
