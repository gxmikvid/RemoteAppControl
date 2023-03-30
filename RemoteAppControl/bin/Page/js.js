//page buttons
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
//misc functions
function startStop(index)
{
    socket.send(index);
}

function buildButtons(ammount)
{
    var contentCont = document.getElementsByClassName("content")[0];
    for (let index = 0; index < ammount; index++) {
        var button = document.createElement("div");
        button.className = "process";
        button.id = index;
        contentCont.appendChild(button);

        var logo = document.createElement("img");
        logo.className = "logo";
        logo.src = "icons/icon"+index+".ico";
        button.appendChild(logo);

        var status = document.createElement("img");
        status.className = "status";
        status.src = "stopped.png";
        status.style.backgroundColor = "rgb(200, 100, 100)";
        button.appendChild(status);
    }

    var processDivs = Array.prototype.slice.call(document.getElementsByClassName("process")) ;
    for (let index = 0; index < processDivs.length; index++) {
        processDivs[index].setAttribute("onclick","startStop("+index+")");
    }
}

function lerpColor(a, b, amount) {
    var ar = a >> 16,
        ag = a >> 8 & 0xff,
        ab = a & 0xff,
        br = b >> 16,
        bg = b >> 8 & 0xff,
        bb = b & 0xff,
        rr = ar + amount * (br - ar),
        rg = ag + amount * (bg - ag),
        rb = ab + amount * (bb - ab);
  
    return '#' + ((1 << 24) + (rr << 16) + (rg << 8) + rb).toString(16).slice(1);
  }
//data handling
var initialised = false;
var buffer = "";
let reader = new FileReader();

reader.onload = function(event) {
  buffer = event.target.result;
};
//socket definitions
var socket = new WebSocket("ws://localhost:8080/");

socket.onopen = function(event) {
    console.log("Connected to server");
};

socket.onmessage = function(event) {
    reader.readAsText(event.data);
    if (buffer.length > 0)
    {
        if(initialised)
        {
            //change status logo
            let rows = buffer.split("\n");
            let split = rows.map(row => row.split(";"));
            for (let index = 0; index < split[0].length; index++) {
                var processStatus = document.getElementById(index).getElementsByClassName("status")[0];
                if(split[0][index] == "1")
                {
                    processStatus.src = "running.png";
                    processStatus.style.backgroundColor = "rgb(100, 200, 100)";
                }
                else
                {
                    processStatus.src = "stopped.png";
                    processStatus.style.backgroundColor = "rgb(200, 100, 100)";
                }
            }
            var CPUDiv = document.getElementById("CPU").getElementsByClassName("resourceUsageVal")[0];
            var RAMDiv = document.getElementById("RAM").getElementsByClassName("resourceUsageVal")[0];
            var cpuval = parseInt(split[1][0]);
            var ramval = parseInt(split[1][1]);
            
            CPUDiv.style.width = cpuval+"%";
            CPUDiv.style.backgroundColor = lerpColor(0x00FF00, 0xFF0000, cpuval/100);
            CPUDiv.innerHTML = cpuval+"%";
            
            RAMDiv.style.width = ramval+"%";
            RAMDiv.style.backgroundColor = lerpColor(0x00FF00, 0xFF0000, ramval/100);
            RAMDiv.innerHTML = ramval+"%";
        }
        else
        {
            buildButtons(parseInt(buffer));
            initialised = true
        }
    }
};

socket.onerror = function(event) {
    console.log("WebSocket error: " + event);
};