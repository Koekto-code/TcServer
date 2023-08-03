
Array.from(document.querySelectorAll('.ctxmenu')).forEach(menu => {
	menu.addEventListener('click', (event) => {
		event.stopPropagation();
	});

	const ov = document.getElementById("shade-overlay");
	let callback = null;

	if (ov !== null && menu.classList.contains("shade-menu")) {
		callback = (event) => {
			if (!menu.contains(event.target)) {
				menu.removeAttribute("menu-enabled");
				ov.removeAttribute("ov-enabled");
			}
		};
	} else {
		callback = (event) => {
			if (!menu.contains(event.target)) {
				menu.removeAttribute("menu-enabled");
			}
		};
	}
	document.addEventListener('click', callback);
})

function addContextMenu(menu, caller)
{
	if (!menu || !caller)
		return;
	
	const ov = document.getElementById("shade-overlay");
	if (menu.classList.contains("shade-menu"))
	{
		if (ov !== null) {
			caller.addEventListener('click', (event) => {
				event.preventDefault();
				menu.setAttribute("menu-enabled", "true");
				ov.setAttribute("ov-enabled", "true");
				event.stopPropagation();
			});
		} else {
			caller.addEventListener('click', (event) => {
				event.preventDefault();
				menu.setAttribute("menu-enabled", "true");
				event.stopPropagation();
			});
		}
	}
	else
	{
		caller.addEventListener('click', (event) => {
			event.preventDefault();
			const rect = caller.getBoundingClientRect();
			menu.style.top = rect.top + rect.height + 'px';
			menu.style.left = rect.left + 'px';
			menu.setAttribute("menu-enabled", "true");
			event.stopPropagation();
		});
		window.addEventListener('resize', () => {
			const rect = caller.getBoundingClientRect();
			menu.style.top = rect.top + rect.height + 'px';
			menu.style.left = rect.left + 'px';
		});
	}
}
