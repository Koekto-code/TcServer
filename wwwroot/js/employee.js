const stImages = document.querySelectorAll('.imgsel');
const selected = new Set();

Array.from(stImages).forEach(img => {
	img.addEventListener('click', (ev) => {
		if (!ev.target.dataset.value)
			return;
		if (!selected.has(ev.target)) {
			selected.add(ev.target);
			ev.target.style = "width: 90%; height: 90%; opacity: 70%; margin: 5%";
		} else {
			selected.delete(ev.target);
			ev.target.style = "";
		}
	})
});

function addSelectedPhotos()
{
	let workers = selected.size;
	const options = {
		method: 'POST'
	};
	
	selected.forEach(img => {
		fetch(`/employees/addphoto?compid=${companyId}&emplid=${employeeId}&photoid=${img.dataset.value}`, options)
		.then((response) => {
			if (!response.ok) {
				throw new Error(`Fetch error: ${response.status}`);
			}
			return response.blob();
		})
		.then(() => {
			--workers;
			if (workers == 0)
				window.location.reload();
		});
	});
}

const cfold = document.getElementById('fold1');
const ucfold = document.getElementById('ftab1cc');
const utfold = document.getElementById('ftab1c');
const tfold = document.getElementById('ftab1');
if (cfold && ucfold && utfold && tfold)
{
	tfold.dataset.value = "collapse";

	cfold.addEventListener('click', () => {
		if (!utfold.dataset.value) {
			ucfold.dataset.value = "open";
			utfold.dataset.value = "open";
			tfold.dataset.value = "";
		}
		else {
			utfold.dataset.value = "";
			setTimeout(() => {
				ucfold.dataset.value = "";
				tfold.dataset.value = "collapse";
			}, 200);
		}
	});

	cfold.addEventListener('mouseenter', () => {
		ucfold.style.backgroundColor = "#fafafe";
	})

	cfold.addEventListener('mouseleave', () => {
		ucfold.style.backgroundColor = "#fff";
	})
}