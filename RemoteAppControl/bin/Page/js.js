var appsBtn = document.getElementById("apps");
var resourcesBtn = document.getElementById("resources");
var appsCont = document.getElementById("appscont");
var resourcesCont = document.getElementById("resourcescont");

appsBtn.onclick = function()
{
    resourcesCont.style.display = "none";
    resourcesBtn.className = "";
    appsCont.style.display = "block";
    appsBtn.className = "activePage";
}

resourcesBtn.onclick = function()
{
    resourcesCont.style.display = "block";
    resourcesBtn.className = "activePage";
    appsCont.style.display = "none";
    appsBtn.className = "";
}

var socket = new WebSocket("ws://localhost:8080/");

socket.onopen = function(event) {
    console.log("Connected to server");
};

socket.onmessage = function(event) {
    console.log("Received message: " + event.data);
};

socket.onerror = function(event) {
    console.log("WebSocket error: " + event);
};

/*var sendButton = document.getElementById("send-button");
sendButton.onclick = function () {
    socket.send("Hello, server!\n");
};*/