
addContextMenu(document.getElementById('dev-add-menu'), document.getElementById('dev-add-btn'));

function toggleDevSelection(val)
{
	const checkboxes = Array.from(document.getElementsByName('dev-select'))
	checkboxes.forEach(cb => {
		cb.checked = val;
	});
}

function addDevice(addr, pass)
{
	const options = {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify({
			'addr': addr,
			'pass': pass,
		})
	};
	return fetch(`/manage/devices/add?compname=${compName}`, options);
}

function addDeviceDyn(event, addr, pass)
{
	if (event.key != 'Enter')
		return;
	
	addDevice(addr, pass).then((re) => {
		if (!re.ok) {
			event.target.classList.add('redfont-light');
		} else  {
			event.target.classList.remove('redfont-light');
		}
	});
}

function addDeviceByMenu()
{
	const addr = document.getElementById('dev-sel-addr');
	const pass = document.getElementById('dev-sel-pass');
	
	if (!addr.value) {
		addr.classList.add('redfont-light');
		return;
	}
	if (!pass.value) {
		pass.classList.add('redfont-light');
		return;
	}
	addDevice(addr.value, pass.value).then((re) => {
		if (!re.ok) {
			addr.classList.add('redfont-light');
		} else {
			window.location.reload();
		}
	});
}
document.getElementById('dev-sel-addr').addEventListener('input', (ev) => {
	ev.target.classList.remove('redfont-light');
});
document.getElementById('dev-sel-addr').addEventListener('input', (ev) => {
	ev.target.classList.remove('redfont-light');
});

function deleteSelectedDevices()
{
	const checkboxes = Array.from(document.getElementsByName('dev-select'))
	const values = []
	checkboxes.forEach(cb => {
		if (cb.checked)
			values.push(cb.value);
	});

	const options = {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(values)
	};

	fetch(`/manage/devices/delete?compname=${compName}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		checkboxes.forEach(cb => {
			cb.checked = false;
		});
		window.location.reload();
	});
}

function resetSelectedDevicesCallback()
{
	const checkboxes = Array.from(document.getElementsByName('dev-select'))
	const values = []
	checkboxes.forEach(cb => {
		if (cb.checked)
			values.push(cb.value);
	});

	const options = {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(values)
	};

	fetch(`/manage/devices/resetcallback?compname=${compName}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		checkboxes.forEach(cb => {
			cb.checked = false;
		});
		window.location.reload();
	});
}