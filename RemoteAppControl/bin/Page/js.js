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