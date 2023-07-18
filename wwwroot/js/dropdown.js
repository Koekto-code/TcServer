// Dropdown menu logic

function ddAddLabelLogic(label, ontextchange)
{
    const titleId = label.getAttribute("dd-header");
    if (titleId)
    {
        const title = document.getElementById(titleId);
        const parent = document.getElementById(title.getAttribute("dd-parent"));
        
        label.addEventListener("click", (event) =>
        {
            const lbl = event.target;
            title.textContent = lbl.textContent;
            
            const ddVal = lbl.getAttribute("dd-value") || lbl.textContent;
            title.setAttribute("dd-value", ddVal);
            parent.setAttribute("dd-show", "");
            
            if (ontextchange)
                ontextchange(title)
        })
    }
    // otherwise it's dummy first item
}

document.querySelectorAll(".dd-label").forEach(label => {
    ddAddLabelLogic(label);
})

document.querySelectorAll(".dd-title").forEach(title =>
{
    const parent = document.getElementById(title.getAttribute("dd-parent"))
    
    // Show/hide items if clicked by title
    title.addEventListener("click", () =>
    {
        if (parent.getAttribute("dd-show") == "true") {
            parent.setAttribute("dd-show", "")
        } else {
            parent.setAttribute("dd-show", "true")
        }
    })
    
    // collapse items if clicked beside the dropdown menu
    document.addEventListener("click", (event) =>
    {
        if (!event.target.closest(".dropdown"))
            parent.setAttribute("dd-show", "")
    })
})
