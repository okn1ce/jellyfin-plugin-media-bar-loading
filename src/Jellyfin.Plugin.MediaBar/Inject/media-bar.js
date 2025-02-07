function saveCredentialsToSessionStorage(credentials) {
    try {
        sessionStorage.setItem(
            "json-credentials",
            JSON.stringify(credentials)
        );
        console.log("Credentials saved to sessionStorage.");
    } catch (error) {
        console.error("Error saving credentials:", error);
    }
}
function saveApiKey(apiKey) {
    try {
        sessionStorage.setItem("api-key", apiKey);
        console.log("API key saved to sessionStorage.");
    } catch (error) {
        console.error("Error saving API key:", error);
    }
}
(function () {
    var originalConsoleLog = console.log;
    console.log = function (message) {
        originalConsoleLog.apply(console, arguments);
        if (
            typeof message === "string" &&
            message.startsWith("Stored JSON credentials:")
        ) {
            try {
                var jsonString = message.substring(
                    "Stored JSON credentials: ".length
                );
                var credentials = JSON.parse(jsonString);
                saveCredentialsToSessionStorage(credentials);
            } catch (error) {
                console.error("Error parsing credentials:", error);
            }
        }
        if (
            typeof message === "string" &&
            message.startsWith("opening web socket with url:")
        ) {
            try {
                var url = message.split("url:")[1].trim();
                var urlParams = new URL(url).searchParams;
                var apiKey = urlParams.get("api_key");
                if (apiKey) {
                    saveApiKey(apiKey);
                }
            } catch (error) {
                console.error("Error extracting API key:", error);
            }
        }
    };
})();