(function () {
    // List of CSS selectors for Home buttons
    const buttonSelectors = [
        ".headerHomeButton.barsMenuButton",
        ".css-17c09up",
        ".mainDrawer-scrollContainer > a:nth-child(2)"
    ];

    // Polling interval to check for buttons
    const intervalId = setInterval(function () {
        buttonSelectors.forEach(selector => {
            // Try to find the button
            const homeButton = document.querySelector(selector);

            // If the button is found
            if (homeButton) {
                // Attach the click event listener if not already added
                if (!homeButton.hasAttribute("data-home-handler")) {
                    homeButton.addEventListener("click", function (event) {
                        event.preventDefault(); // Prevent default behavior if necessary
                        window.location.href = "/web/index.html#/home.html";
                    });

                    // Mark the button as handled
                    homeButton.setAttribute("data-home-handler", "true");
                }
            }
        });

        // Stop polling if all buttons are found
        if (buttonSelectors.every(selector => document.querySelector(selector))) {
            clearInterval(intervalId);
        }
    }, 100); // Check every 100ms
})();