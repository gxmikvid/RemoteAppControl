var socket = new WebSocket("ws://127.0.0.1:8080");

socket.onopen = function (event) {
    console.log("Connected to server");
};

socket.onmessage = function (event) {
    console.log("Received message: " + event.data);
};

var sendButton = document.getElementById("send-button");
sendButton.onclick = function () {
    socket.send("Hello, server!\n");
};