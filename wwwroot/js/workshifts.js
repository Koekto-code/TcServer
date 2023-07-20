
addContextMenu(document.getElementById('workshift-add-menu'), document.getElementById('workshift-add-btn'));

function toggleSelection(val) {
	document.getElementsByName('shift-select').forEach((cb) => {
		cb.checked = val;
	});
}

function deleteSelected() {
	let values = new Array();
	document.getElementsByName('shift-select').forEach((cb) => {
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
	
	fetch(`/manage/workshifts/delete?compname=${compName}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		window.location.reload();
	})
}

const daytimeInputs = document.querySelectorAll('.daytime-ti');
daytimeInputs.forEach((elem) => {
	elem.addEventListener('input', (ev) => {
		const ti = ev.target;
		ti.classList.remove('red-border');
		ti.classList.remove('redfont-light');
		if (ti.value.length > 2) {
			let l = ti.value.substring(0, 2);
			let r = ti.value.charAt(2) != ':' ? ti.value.substring(2) : ti.value.length > 3 ? ti.value.substring(3) : '';
			if (r.length > 2)
				r = r.substring(0, 2);
			ti.value = l + ':' + r;
		}
	});
});

function addWorkshift()
{
	let valid = true;
	
	const getval = (id) => {
		const elem = document.getElementById(id);
		if (!elem.value)
			return null;
		
		const parts = elem.value.split(':');
		const time = parts.length != 2 ? -1 : parseInt(parts[0]) * 60 + parseInt(parts[1]);
		
		if (!(time >= 0 && time < 1440))
		{
			elem.classList.add('red-border');
			elem.classList.add('redfont-light');
			valid = false;
			return null;
		}
		return time;
	}
	const getfield = (id) => {
		const first = getval(id + 'b');
		const second = getval(id + 'e');
		
		if ((first == null) != (second == null)) {
			valid = false;
			const inv_id = !first ? id + 'b' : id + 'e';
			document.getElementById(inv_id).classList.add('red-border');
			return null;
		}
		return { first: first, second: second };
	}
	
	const mon = getfield('dtti_1');
	const tue = getfield('dtti_2');
	const wed = getfield('dtti_3');
	const thu = getfield('dtti_4');
	const fri = getfield('dtti_5');
	const sat = getfield('dtti_6');
	const sun = getfield('dtti_7');
	
	if (!valid)
		return;
	
	const jobtitle_ti = document.getElementById('jobtitle_ti');
	if (!jobtitle_ti.value) {
		jobtitle_ti.classList.add('redfont-light');
		return;
	}
	
	const dto = {
		JobTitle: jobtitle_ti.value,
		
		DateBegin: document.getElementById('workshift-beg-di').value || null,
		DateEnd: document.getElementById('workshift-end-di').value || null,
		
		MonArrive: mon.first,
		MonLeave: mon.second,
		
		TueArrive: tue.first,
		TueLeave: tue.second,
		
		WedArrive: wed.first,
		WedLeave: wed.second,
		
		ThuArrive: thu.first,
		ThuLeave: thu.second,
		
		FriArrive: fri.first,
		FriLeave: fri.second,
		
		SatArrive: sat.first,
		SatLeave: sat.second,
		
		SunArrive: sun.first,
		SunLeave: sun.second
	};
	
	const options = {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(dto)
	};
	
	console.log(options);

	fetch(`/manage/workshifts/add?compname=${compName}`, options)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		window.location.reload();
	});
}
document.getElementById('jobtitle_ti').addEventListener('input', (ev) => {
	ev.target.classList.remove('redfont-light');
});