
addContextMenu(document.getElementById('menu1'), document.getElementById('dots-btn1'));
addContextMenu(document.getElementById('excel-export-menu'), document.getElementById('excel-export-btn'));
addContextMenu(document.getElementById('admin-pwd-reset-menu'), document.getElementById('admin-pwd-reset-btn'));

function employeePage(innerid) {
	window.location.href = `/employee?compname=${compName}&id=${innerid}`;
}

function changeUnit(unitname) {
	window.location.href = `/schedule?compname=${compName}&unitname=${unitname}&viewdate=${viewDate}`;
}

function changeComp(compname) {
	window.location.href = `/schedule?compname=${compname}&viewdate=${viewDate}`;
}

function changeDate(newdate) {
	window.location.href = `/schedule?compname=${compName}&unitname=${unitName}&viewdate=${newdate}`;
}

function getExcelSheet() {
	window.location.href = `/schedule/xlsxreport?compname=${compName}&unitname=${unitName}&viewdate=${viewDate}`;
}

function delCookie(name) {
	document.cookie = name + '=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT;'
}

function accountExit() {
	delCookie("scheduleCompName");
	delCookie("scheduleUnitName");
	window.location.href = "/logout";
}

function resetView() {
	delCookie("scheduleCompName");
	delCookie("scheduleUnitName");
	window.location.href = "/schedule";
}

function toggleEmplSelection(val) {
	document.getElementsByName('empl-select').forEach(cb => {
		cb.checked = val;
	});
}

document.getElementById('empl-sel-title').addEventListener("input", (ev) => {
	ev.target.style.color = "black";
});

document.getElementById('empl-sel-name').addEventListener("input", (ev) => {
	ev.target.style.color = "black";
});

function createEmployee()
{
	const empltitle = document.getElementById('empl-sel-title');
	const emplname = document.getElementById('empl-sel-name');
	const empladdr = document.getElementById('empl-sel-addr');
	const emplphone = document.getElementById('empl-sel-phone');
	
	if (!empltitle.value)
		empltitle.style.color = "red";

	if (!emplname.value)
		emplname.style.color = "red";

	if (!empltitle.value || !emplname.value)
		return;

	const init = {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify({
			'Name': emplname.value,
			'JobTitle': empltitle.value,
			'HomeAddress': empladdr.value,
			'Phone': emplphone.value
		})
	};

	fetch(`/manage/employees/add?unitid=${unitId}`, init)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		window.location.reload();
	});
}

function deleteSelectedEmployees()
{
	const checkboxes = document.getElementsByName('empl-select');
	const values = []
	checkboxes.forEach(cb => {
		if (cb.checked)
			values.push(cb.value);
	});
	
	if (!window.confirm(`Удалить все данные выбранных сотрудников (${values.length})?`))
		return;

	const init = {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(values)
	};

	fetch(`/manage/employees/delete?compname=${compName}`, init)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		checkboxes.forEach(cb => {
			cb.checked = false;
		});
		window.location.reload();
	});
}

function transferEmployees(destination)
{
	const checkboxes = document.getElementsByName('empl-select');
	const values = []
	checkboxes.forEach(cb => {
		if (cb.checked)
			values.push(cb.value);
	});

	const init = {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(values)
	};

	fetch(`/manage/employees/transfer?compname=${compName}&destination=${destination}`, init)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		checkboxes.forEach(cb => {
			cb.checked = false;
		});
		window.location.reload();
	});
}

function createUnit(name)
{
	const init = {
		method: 'POST'
	};

	fetch(`/manage/unit/add?compname=${compName}&name=${name}`, init)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		window.location.reload();
	});
}

function deleteCurrentComp()
{
	if (!window.confirm("Удалить текущую организацию?"))
		return;
	
	const init = {
		method: 'POST'
	};

	fetch(`/manage/comp/delete?compname=${compName}`, init)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		window.location.href = "/schedule";
	});
}

function deleteCurrentUnit()
{
	if (!window.confirm("Удалить текущий отдел?"))
		return;
	
	const init = {
		method: 'POST'
	};

	fetch(`/manage/unit/delete?compname=${compName}&unitname=${unitName}`, init)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		changeComp(compName);
	});
}

function renameUnit(newname)
{
	const init = {
		method: 'POST'
	};

	fetch(`/manage/unit/rename?compname=${compName}&unitname=${unitName}&newname=${newname}`, init)
	.then((response) => {
		if (!response.ok)
			throw new Error(`Fetch error: ${response.status}`);
		changeUnit(newname);
	});
}

function exportCustomExcel()
{
	const ddex1 = document.getElementById('ddex1');
	const ddex2 = document.getElementById('ddex2');
	const ddex = document.getElementById('ddex_h');
	
	if (!ddex1.value)
		return;

	const ddexval = ddex.getAttribute("dd-value");

	let link = `/schedule/xlsxreport?compname=${compName}&viewdate=${ddex1.value}`;
	if (ddexval && (ddexval != "_all_"))
		link += `&unitname=${ddexval}`;
	if (ddex2.value)
		link += `&dateend=${ddex2.value}`;
	window.location.href = link;
}

function pwdValidation(pass, pass1, btn)
{
	btn.style.pointerEvents = "none";
	const prevBgColor = btn.style.backgroundColor;
	btn.style.backgroundColor = "#aaa";
	
	const validate = () => {
		if (pass.value == pass1.value) {
			btn.style.backgroundColor = prevBgColor;
			btn.style.pointerEvents = "all";
		} else {
			btn.style.backgroundColor = "#aaa";
			btn.style.pointerEvents = "none";
		}
	};
	
	pass.addEventListener('input', validate);
	pass1.addEventListener('input', validate);
}

// Company adding logic
{
	const pass = document.getElementById('comp-sel-pass');
	const pass1 = document.getElementById('comp-sel-pass1');
	const submit = document.getElementById('comp-submit-btn');
	const form = document.getElementById('comp-create-form');
	
	pwdValidation(pass, pass1, submit);
	
	form.addEventListener('submit', (ev) => {
		ev.preventDefault();

		var formData = new FormData(form);

		var request = new Request('/register', {
			method: 'POST',
			headers: {
				'Content-Type': 'application/x-www-form-urlencoded'
			},
			body: new URLSearchParams(formData)
		});

		fetch(request)
		.then((response) => {
			if (!response.ok)
				throw new Error(`Fetch error: ${response.status}`);
			window.location.reload();
		})
	});
}

// Password resetting
{
	const pass = document.getElementById('pwd-reset-sel-pass');
	const pass1 = document.getElementById('pwd-reset-sel-pass1');
	const submit = document.getElementById('pwd-reset-submit-btn');
	const form = document.getElementById('admin-pwd-reset-form');
	
	pwdValidation(pass, pass1, submit);
	
	form.addEventListener('submit', (ev) => {
		ev.preventDefault();

		var formData = new FormData(form);

		var request = new Request('/resetpwd', {
			method: 'POST',
			headers: {
				'Content-Type': 'application/x-www-form-urlencoded'
			},
			body: new URLSearchParams(formData)
		});

		fetch(request)
		.then((response) => {
			if (!response.ok)
				throw new Error(`Fetch error: ${response.status}`);
			window.location.reload();
		})
	});
}

// Unit creating logic
{
	const textinp = document.getElementById('unit-create-ti');
	if (textinp)
	{
		textinp.addEventListener('keypress', (ev) => {
			if (ev.key != 'Enter')
				return;
			createUnit(ev.target.value);
		});
	}
}

// Unit renaming logic
{
	const textinp = document.getElementById('unit-rename-ti');
	if (textinp)
	{
		textinp.addEventListener('keypress', (ev) => {
			if (ev.key != 'Enter')
				return;
			renameUnit(ev.target.value);
		});
	}
}

// "Search by filter" logic
{
	const tbody = document.getElementById('tbody1');
	const trows = Array.from(tbody.querySelectorAll('tr'));
	const filter = document.getElementById('filter1');

	filter.addEventListener('input', () => {
		const filteredRows = trows.filter(row => {
			if (!filter.value)
				return true;
			const fst = row.querySelector("td:first-child").innerText.toLowerCase();
			const snd = row.querySelector("td:nth-child(2)").innerText.toLowerCase();
			const trd = row.querySelector("td:nth-child(3)").innerText.toLowerCase();
			if (! (fst || snd || trd)) return true;
			let ok = false;
			
			const fval = filter.value.toLowerCase();
			if (fst) ok |= fst.includes(fval);
			if (snd) ok |= snd.includes(fval);
			if (trd) ok |= trd.includes(fval);
			return ok;
		});
		tbody.innerHTML = "";
		filteredRows.forEach(row => {
			tbody.appendChild(row);
		})
	});
}

// adjust font size for dropdown items
{
	const adjustHeight = async (elem) => {
		if (elem.scrollHeight > 16)
			elem.style.fontSize = '12px';
	}

	const items0 = document.querySelectorAll('.dd-label[dd-header="dd0_h"]');
	items0.forEach(adjustHeight);

	const items1 = document.querySelectorAll('.dd-label[dd-header="dd1_h"]');
	items1.forEach(adjustHeight);
}