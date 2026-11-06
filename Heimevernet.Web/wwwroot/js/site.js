// Felles JavaScript for KriseKlar.
// Alt her skjer i nettleseren. Posisjonen til brukeren sendes aldri til serveren.
(function () {
    'use strict';

    // ---------- Hjelpefunksjoner ----------

    // Avstand i meter mellom to punkter. Haversine-formelen regner jorda som en kule (radius 6371 km),
    // som er mer enn nøyaktig nok for avstander i Norge.
    function distanceMeters(lat1, lng1, lat2, lng2) {
        var toRad = Math.PI / 180;
        var dLat = (lat2 - lat1) * toRad;
        var dLng = (lng2 - lng1) * toRad;
        var a = Math.sin(dLat / 2) * Math.sin(dLat / 2)
            + Math.cos(lat1 * toRad) * Math.cos(lat2 * toRad) * Math.sin(dLng / 2) * Math.sin(dLng / 2);
        return 6371000 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }

    // 850 m, 1,2 km, 37 km
    function formatDistance(meters) {
        if (meters < 1000) { return Math.max(10, Math.round(meters / 10) * 10) + ' m'; }
        if (meters < 10000) { return String(Math.round(meters / 100) / 10).replace('.', ',') + ' km'; }
        return Math.round(meters / 1000) + ' km';
    }

    function toArray(list) { return Array.prototype.slice.call(list); }

    // ---------- Brukerens posisjon ----------
    // Nettleseren spør brukeren om lov. Posisjonen huskes i sessionStorage, som slettes når fanen lukkes.

    var position = null;        // { lat, lng } eller null
    var positionState = 'unknown'; // unknown | asking | known | denied | unavailable
    var positionListeners = [];

    try {
        var saved = JSON.parse(sessionStorage.getItem('kk-position'));
        if (saved && typeof saved.lat === 'number' && typeof saved.lng === 'number') {
            position = saved;
            positionState = 'known';
        }
    } catch (e) { /* sessionStorage kan være slått av. Da spør vi på nytt. */ }

    function notifyPosition() {
        positionListeners.forEach(function (listener) { listener(position, positionState); });
    }

    function requestPosition() {
        // Nettlesere gir bare posisjon på sikre sider: https, eller localhost. Åpnes appen som
        // http://192.168.x.x (f.eks. fra mobilen på samme Wi-Fi), spør nettleseren aldri, så vi sier fra i stedet.
        if (!window.isSecureContext) {
            positionState = 'insecure';
            notifyPosition();
            return;
        }
        if (!navigator.geolocation) {
            positionState = 'unavailable';
            notifyPosition();
            return;
        }
        positionState = 'asking';
        notifyPosition();
        navigator.geolocation.getCurrentPosition(function (result) {
            position = { lat: result.coords.latitude, lng: result.coords.longitude };
            positionState = 'known';
            try { sessionStorage.setItem('kk-position', JSON.stringify(position)); } catch (e) { }
            notifyPosition();
        }, function (error) {
            positionState = error.code === 1 ? 'denied' : 'unavailable';
            notifyPosition();
        }, { enableHighAccuracy: false, timeout: 15000, maximumAge: 300000 });
    }

    window.kriseklarPosition = {
        get: function () { return position; },
        request: requestPosition,
        onChange: function (listener) { positionListeners.push(listener); }
    };

    // Teksten "Avstand regnes fra posisjonen din" og knappen "Bruk min posisjon".
    // Markup: [data-location-status] med [data-location-text] og knappen [data-use-location] inni.
    var statusTexts = {
        unknown: ['Slå på posisjon for å se hvor langt unna ressursene er.', 'Bruk min posisjon'],
        asking: ['Henter posisjonen din …', 'Bruk min posisjon'],
        known: ['Avstanden regnes fra der du er nå.', 'Oppdater posisjon'],
        denied: ['Du har ikke gitt tilgang til posisjonen din, så avstand vises ikke.', 'Prøv igjen'],
        unavailable: ['Fant ikke posisjonen din, så avstand vises ikke.', 'Prøv igjen'],
        insecure: ['Posisjon virker bare over https eller localhost, så avstand vises ikke på denne adressen.', 'Prøv igjen']
    };
    function updateLocationStatus() {
        document.querySelectorAll('[data-location-status]').forEach(function (element) {
            var texts = statusTexts[positionState];
            var text = element.querySelector('[data-location-text]');
            var button = element.querySelector('[data-use-location]');
            if (text) { text.textContent = texts[0]; }
            if (button) {
                button.textContent = texts[1];
                button.disabled = positionState === 'asking';
            }
        });
    }
    document.querySelectorAll('[data-use-location]').forEach(function (button) {
        button.addEventListener('click', requestPosition);
    });
    positionListeners.push(updateLocationStatus);
    updateLocationStatus();

    // Spør om posisjon med en gang appen åpnes (alle sider etter innlogging, [data-sidebar]), så kartet,
    // avstandene og "Nærmest deg" er klare. Ikke på innloggingssiden, og ikke hvis brukeren allerede har sagt nei.
    // Svaret huskes av nettleseren, så spørsmålet kommer bare første gang.
    if (!position && (document.querySelector('[data-sidebar]') || document.querySelector('[data-location-status]'))) {
        if (navigator.permissions && navigator.permissions.query) {
            navigator.permissions.query({ name: 'geolocation' }).then(function (result) {
                if (result.state === 'denied') {
                    positionState = 'denied';
                    notifyPosition();
                } else {
                    requestPosition();
                }
                // Gir brukeren tilgang senere (i innstillingene til nettleseren), hentes posisjonen med en gang.
                result.onchange = function () {
                    if (result.state === 'granted') { requestPosition(); }
                };
            }, requestPosition);
        } else {
            requestPosition();
        }
    }

    // ---------- Søk og filtre på ressurslister ----------
    // Markup: et element med data-resource-filter. Inni det (alle valgfrie unntatt elementene):
    //   input[data-filter-search]                søkefelt
    //   button[data-filter-chip][data-category]  kategori (aria-pressed), kan ha [data-chip-count] inni
    //   button[data-filter-distance="1000"]      maks avstand i meter, 0 = alle (aria-checked)
    //   select[data-filter-place]                by / poststed
    //   input[data-filter-available]             bare tilgjengelige
    //   select[data-filter-sort]                 distance | newest | name
    //   button[data-filter-reset]                nullstill
    //   [data-filter-count] [data-filter-summary] [data-filter-empty] [data-filter-walk]
    // Hver ressurs er et [data-filter-item] med data-category, data-search, data-place, data-status,
    // data-registered, data-name og eventuelt data-lat/data-lng. [data-distance] inni får avstanden.

    var walkTexts = {
        '0': 'Viser ressurser uansett avstand.',
        '1000': 'Ca. 13 minutter å gå.',
        '5000': 'Ca. 1 time å gå, eller 20 minutter på sykkel.',
        '10000': 'Ca. 15 minutter med bil.',
        '25000': 'Ca. 30 minutter med bil.'
    };
    var sortTexts = { distance: 'nærmest først', newest: 'nyeste først', name: 'etter navn' };

    function initResourceFilter(root) {
        var one = function (selector) { return root.querySelector(selector); };
        var all = function (selector) { return toArray(root.querySelectorAll(selector)); };

        var search = one('[data-filter-search]');
        var chips = all('[data-filter-chip]');
        var distances = all('[data-filter-distance]');
        var place = one('[data-filter-place]');
        var available = one('[data-filter-available]');
        var sort = one('[data-filter-sort]');
        var items = all('[data-filter-item]');
        var count = one('[data-filter-count]');
        var summary = one('[data-filter-summary]');
        var empty = one('[data-filter-empty]');
        var walk = one('[data-filter-walk]');
        var countWord = root.dataset.countWord || 'ressurs';

        function itemDistance(item) {
            if (!position || item.dataset.lat === undefined || item.dataset.lat === '') { return null; }
            return distanceMeters(position.lat, position.lng, parseFloat(item.dataset.lat), parseFloat(item.dataset.lng));
        }

        function maxDistance() {
            var checked = distances.filter(function (button) { return button.getAttribute('aria-checked') === 'true'; })[0];
            return checked ? parseInt(checked.dataset.filterDistance, 10) : 0;
        }

        function sortItems(mode) {
            var sorted = items.slice().sort(function (a, b) {
                if (mode === 'newest') {
                    return (b.dataset.registered || '').localeCompare(a.dataset.registered || '');
                }
                if (mode === 'distance' && (a.kkDistance !== null || b.kkDistance !== null)) {
                    if (a.kkDistance === null) { return 1; }  // Uten punkt på kartet: til slutt.
                    if (b.kkDistance === null) { return -1; }
                    return a.kkDistance - b.kkDistance;
                }
                return (a.dataset.name || '').localeCompare(b.dataset.name || '', 'nb');
            });
            sorted.forEach(function (item) { item.parentNode.appendChild(item); });
        }

        function apply() {
            var text = search ? search.value.trim().toLowerCase() : '';
            var activeCategories = chips
                .filter(function (chip) { return chip.getAttribute('aria-pressed') === 'true'; })
                .map(function (chip) { return chip.dataset.category; });
            var limit = maxDistance();
            var placeValue = place ? place.value : '';
            var onlyAvailable = available ? available.checked : false;
            var categoryCounts = {};
            var visible = [];

            items.forEach(function (item) {
                var distance = itemDistance(item);
                item.kkDistance = distance;
                item.querySelectorAll('[data-distance]').forEach(function (label) {
                    label.textContent = distance === null ? '' : formatDistance(distance);
                    label.hidden = distance === null;
                });

                // Uten posisjon kan vi ikke filtrere på avstand, så da vises alt.
                var distanceOk = limit === 0 || !position || (distance !== null && distance <= limit);
                var otherOk = distanceOk
                    && (text === '' || (item.dataset.search || '').indexOf(text) >= 0)
                    && (placeValue === '' || item.dataset.place === placeValue)
                    && (!onlyAvailable || item.dataset.status === 'tilgjengelig');
                if (otherOk) {
                    categoryCounts[item.dataset.category] = (categoryCounts[item.dataset.category] || 0) + 1;
                }

                var categoryOk = activeCategories.length === 0 || activeCategories.indexOf(item.dataset.category) >= 0;
                item.hidden = !(otherOk && categoryOk);
                if (!item.hidden) { visible.push(item); }
            });

            // Tallet på hver kategoriknapp: hvor mange du får hvis du velger den (med de andre filtrene).
            chips.forEach(function (chip) {
                var badge = chip.querySelector('[data-chip-count]');
                if (badge) { badge.textContent = categoryCounts[chip.dataset.category] || 0; }
            });

            var sortMode = sort ? sort.value : 'distance';
            sortItems(sortMode);

            if (count) {
                count.textContent = visible.length + ' ' + (visible.length === 1 ? countWord : countWord + 'er');
            }
            if (summary) {
                var parts = [];
                parts.push(limit === 0 || !position ? 'alle avstander' : 'innen ' + formatDistance(limit));
                parts.push(activeCategories.length === 0 ? 'alle kategorier' : chips
                    .filter(function (chip) { return chip.getAttribute('aria-pressed') === 'true'; })
                    .map(function (chip) { return chip.dataset.label.toLowerCase(); }).join(', '));
                if (placeValue) { parts.push(placeValue); }
                if (onlyAvailable) { parts.push('bare tilgjengelige'); }
                parts.push(sortMode === 'distance' && !position ? sortTexts.name : sortTexts[sortMode]);
                summary.textContent = '· ' + parts.join(' · ');
            }
            if (empty) { empty.hidden = visible.length !== 0; }
            if (walk) {
                walk.textContent = !position && limit !== 0
                    ? 'Slå på posisjon for å filtrere på avstand.'
                    : walkTexts[String(limit)] || '';
            }

            // Kartet på siden lytter på denne og viser bare markørene som passer filtrene.
            root.dispatchEvent(new CustomEvent('resourcefilter', { detail: { visible: visible } }));
        }

        chips.forEach(function (chip) {
            chip.addEventListener('click', function () {
                chip.setAttribute('aria-pressed', chip.getAttribute('aria-pressed') === 'true' ? 'false' : 'true');
                apply();
            });
        });
        distances.forEach(function (button) {
            button.addEventListener('click', function () {
                distances.forEach(function (other) { other.setAttribute('aria-checked', other === button ? 'true' : 'false'); });
                apply();
            });
        });
        if (search) { search.addEventListener('input', apply); }
        [place, available, sort].forEach(function (control) {
            if (control) { control.addEventListener('change', apply); }
        });
        all('[data-filter-reset]').forEach(function (button) {
            button.addEventListener('click', function () {
                if (search) { search.value = ''; }
                chips.forEach(function (chip) { chip.setAttribute('aria-pressed', 'false'); });
                distances.forEach(function (other) {
                    other.setAttribute('aria-checked', other.dataset.filterDistance === '0' ? 'true' : 'false');
                });
                if (place) { place.value = ''; }
                if (available) { available.checked = false; }
                if (sort) { sort.value = 'distance'; }
                apply();
            });
        });

        root.kriseklarFilter = { apply: apply, items: items };
        positionListeners.push(apply);
        apply();
    }

    // ---------- Kart ----------

    // Alle kart starter i Kristiansand. Deler brukeren posisjonen, flyttes kartet dit.
    // Kartet kan ikke dras utenfor Norge eller zoomes så langt ut at man ser mer enn landet.
    var KRISTIANSAND = [58.1467, 7.9956];
    var START_ZOOM = 12;
    var NORWAY = [[57.7, 4.0], [71.4, 31.6]]; // Sørvest og nordøst for fastlands-Norge.

    // Kartbakgrunn fra OpenStreetMap.
    // ResizeObserver ber Leaflet måle kartet på nytt når boksen endrer størrelse
    // (f.eks. når layouten er ferdig, et skjult kart blir vist eller vinduet blir smalere).
    // Uten dette tegnes kartet bare i en del av boksen.
    window.beredskapTiles = function (map) {
        map.setMaxBounds(L.latLngBounds(NORWAY).pad(0.05));
        map.options.maxBoundsViscosity = 1.0; // Kartet "spretter" ikke utenfor, det stopper ved kanten.
        map.setMinZoom(5);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        }).addTo(map);

        if ('ResizeObserver' in window) {
            new ResizeObserver(function () { map.invalidateSize(); }).observe(map.getContainer());
        }
    };

    // Rund markør i kategorifargen med kategoriikonet, som i designet.
    // Fargen og ikonet kommer fra ResourceCategory på serveren. Vi sjekker likevel formatet,
    // så det aldri kan komme HTML inn i markøren.
    var pinPath = 'M12 21s-7-6.2-7-11a7 7 0 0 1 14 0c0 4.8-7 11-7 11z';
    function markerIcon(color, path) {
        var safeColor = /^#[0-9A-Fa-f]{6}$/.test(color || '') ? color : '#14305A';
        var safePath = /^[MLHVCSQTAZmlhvcsqtaz0-9 .,\-]+$/.test(path || '') ? path : pinPath;
        return L.divIcon({
            className: 'kk-marker',
            html: '<span class="kk-marker-pin" style="background:' + safeColor + '">'
                + '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" '
                + 'stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="' + safePath + '"></path></svg></span>',
            iconSize: [40, 40],
            iconAnchor: [20, 20],
            popupAnchor: [0, -20]
        });
    }
    window.kriseklarMarkerIcon = markerIcon;

    // Markører for alle ressursene i en filterliste (root), og en blå prikk for brukeren.
    // Markørene følger filtrene: det som er skjult i listen, er skjult på kartet.
    window.kriseklarResourceMap = function (map, root) {
        var layer = L.layerGroup().addTo(map);
        var markers = [];
        var items = root.kriseklarFilter ? root.kriseklarFilter.items : toArray(root.querySelectorAll('[data-filter-item]'));

        items.forEach(function (item) {
            if (!item.dataset.lat) { return; }
            var marker = L.marker([parseFloat(item.dataset.lat), parseFloat(item.dataset.lng)], {
                icon: markerIcon(item.dataset.color, item.dataset.icon),
                title: item.dataset.name, // Leses opp av skjermlesere og vises ved hover.
                keyboard: true
            });

            // textContent hindrer at tekst fra brukere tolkes som HTML (XSS).
            marker.bindPopup(function () {
                var popup = document.createElement('div');
                popup.className = 'kk-popup';
                var title = document.createElement('strong');
                title.textContent = item.dataset.name;
                var meta = document.createElement('span');
                meta.textContent = item.dataset.label + (item.kkDistance != null ? ' · ' + formatDistance(item.kkDistance) : '');
                var link = document.createElement('a');
                link.href = item.dataset.url;
                link.textContent = 'Se ressursen';
                popup.append(title, meta, link);
                return popup;
            });
            marker.kkItem = item;
            markers.push(marker);
            marker.addTo(layer);
        });

        root.addEventListener('resourcefilter', function (event) {
            markers.forEach(function (marker) {
                var show = event.detail.visible.indexOf(marker.kkItem) >= 0;
                if (show && !layer.hasLayer(marker)) { layer.addLayer(marker); }
                if (!show && layer.hasLayer(marker)) { layer.removeLayer(marker); }
            });
        });

        var userMarker = null;
        function showUser() {
            if (!position) { return; }
            if (!userMarker) {
                userMarker = L.circleMarker([position.lat, position.lng], {
                    radius: 9, color: '#FFFFFF', weight: 3, fillColor: '#2563EB', fillOpacity: 1
                }).bindTooltip('Du er her').addTo(map);
            } else {
                userMarker.setLatLng([position.lat, position.lng]);
            }
        }
        positionListeners.push(showUser);
        showUser();

        // Første visning: der brukeren er hvis vi vet det, ellers Kristiansand.
        // Kommer posisjonen senere (brukeren svarer på spørsmålet), flyttes kartet dit én gang.
        map.setView(position ? [position.lat, position.lng] : KRISTIANSAND, position ? 13 : START_ZOOM);
        var centeredOnUser = !!position;
        positionListeners.push(function () {
            if (position && !centeredOnUser) {
                centeredOnUser = true;
                map.flyTo([position.lat, position.lng], 13, { duration: 1 });
            }
        });

        if (root.kriseklarFilter) { root.kriseklarFilter.apply(); }
        return { layer: layer, markers: markers };
    };

    // ---------- Velg punkt på et lite kart (skjemaet for ny/endret ressurs) ----------
    // Markup: [data-point-picker] med [data-point-map], input[data-point-lat], input[data-point-lng],
    // [data-point-text], knappene [data-point-mine] og [data-point-clear].
    // Adresse fra koordinater ("reverse geocoding") med Nominatim fra OpenStreetMap.
    // Oppslaget gjøres fra nettleseren, så serveren trenger ikke internett. Nominatim tillater ett oppslag
    // i sekundet, så vi venter til brukeren har sluttet å flytte pinnen (debounce) og avbryter gamle oppslag.
    // Feltene fylles bare hvis de er tomme eller ble fylt ut automatisk før. Det brukeren har skrevet selv, røres ikke.
    function createAddressLookup(form, statusElement) {
        var fields = {
            address: form.querySelector('[name="Address"]'),
            postalCode: form.querySelector('[name="PostalCode"]'),
            postalPlace: form.querySelector('[name="PostalPlace"]')
        };
        if (!fields.address) { return function () { }; }

        Object.keys(fields).forEach(function (key) {
            var input = fields[key];
            if (input) { input.addEventListener('input', function () { delete input.dataset.autofilled; }); }
        });

        function fill(input, value) {
            if (!input || !value) { return; }
            if (input.value.trim() === '' || input.dataset.autofilled === 'true') {
                input.value = value;
                input.dataset.autofilled = 'true';
            }
        }

        var timer = null;
        var controller = null;
        return function (lat, lng) {
            clearTimeout(timer);
            timer = setTimeout(function () {
                if (controller) { controller.abort(); }
                controller = 'AbortController' in window ? new AbortController() : null;
                statusElement.textContent = 'Henter adressen for punktet …';
                var url = 'https://nominatim.openstreetmap.org/reverse?format=jsonv2&addressdetails=1&zoom=18&accept-language=nb'
                    + '&lat=' + lat.toFixed(6) + '&lon=' + lng.toFixed(6);
                fetch(url, { signal: controller ? controller.signal : undefined, headers: { 'Accept': 'application/json' } })
                    .then(function (response) { return response.ok ? response.json() : null; })
                    .then(function (data) {
                        var a = data && data.address;
                        if (!a) { statusElement.textContent = 'Fant ingen adresse her. Skriv den inn selv hvis du vet den.'; return; }
                        var street = [a.road || a.pedestrian || a.footway || a.path || a.hamlet, a.house_number].filter(Boolean).join(' ');
                        var place = a.city || a.town || a.village || a.municipality || a.suburb || '';
                        var postcode = /^\d{4}$/.test(a.postcode || '') ? a.postcode : '';
                        fill(fields.address, street);
                        if (postcode && place) { fill(fields.postalCode, postcode); fill(fields.postalPlace, place); }
                        statusElement.textContent = 'Adresse fra kartet: ' + ([street, (postcode + ' ' + place).trim()].filter(Boolean).join(', ') || 'ukjent')
                            + '. Sjekk at den stemmer.';
                    })
                    .catch(function (error) {
                        if (error && error.name === 'AbortError') { return; }
                        statusElement.textContent = 'Fikk ikke hentet adressen. Skriv den inn selv.';
                    });
            }, 700);
        };
    }

    function initPointPicker(root) {
        var lookupAddress = createAddressLookup(root.closest('form') || document, root.querySelector('[data-point-address]'));
        var latInput = root.querySelector('[data-point-lat]');
        var lngInput = root.querySelector('[data-point-lng]');
        var text = root.querySelector('[data-point-text]');
        var clear = root.querySelector('[data-point-clear]');
        var mine = root.querySelector('[data-point-mine]');
        var map = L.map(root.querySelector('[data-point-map]'));
        beredskapTiles(map);
        var marker = null;

        function setPoint(lat, lng, zoom, skipLookup) {
            // Punktum som desimaltegn, 6 desimaler (ca. 10 cm), som i databasen.
            latInput.value = lat.toFixed(6);
            lngInput.value = lng.toFixed(6);
            if (!marker) {
                marker = L.marker([lat, lng], { icon: markerIcon('#14305A', pinPath), draggable: true, keyboard: true, title: 'Valgt punkt' }).addTo(map);
                marker.on('dragend', function () {
                    var moved = marker.getLatLng();
                    setPoint(moved.lat, moved.lng);
                });
            } else {
                marker.setLatLng([lat, lng]);
            }
            text.textContent = 'Valgt punkt: ' + lat.toFixed(5) + ', ' + lng.toFixed(5) + '. Dra markøren eller klikk et annet sted for å flytte den.';
            clear.hidden = false;
            if (zoom) { map.setView([lat, lng], zoom); }
            if (!skipLookup) { lookupAddress(lat, lng); }
            latInput.dispatchEvent(new Event('change', { bubbles: true }));
        }

        function clearPoint() {
            latInput.value = '';
            lngInput.value = '';
            if (marker) { map.removeLayer(marker); marker = null; }
            text.textContent = 'Ingen punkt valgt. Klikk i kartet der ressursen er.';
            clear.hidden = true;
        }

        map.on('click', function (event) { setPoint(event.latlng.lat, event.latlng.lng); });
        clear.addEventListener('click', clearPoint);

        var waitingForMine = false;
        mine.addEventListener('click', function () {
            waitingForMine = true;
            requestPosition();
        });
        positionListeners.push(function () {
            if (waitingForMine && position) {
                waitingForMine = false;
                setPoint(position.lat, position.lng, 16);
            }
        });

        var lat = parseFloat(latInput.value);
        var lng = parseFloat(lngInput.value);
        if (!isNaN(lat) && !isNaN(lng)) {
            // Punkt fra før (fra kartet på forsiden, eller en ressurs som endres): slå opp adressen
            // bare hvis den mangler, så ressursen blir søkbar på adresse.
            var addressInput = (root.closest('form') || document).querySelector('[name="Address"]');
            setPoint(lat, lng, 15, addressInput && addressInput.value.trim() !== '');
        } else {
            clearPoint();
            map.setView(position ? [position.lat, position.lng] : KRISTIANSAND, position ? 13 : START_ZOOM);
        }
    }

    // ---------- Forhåndsvisning av bildet i skjemaet ----------
    function initImagePreview(input) {
        var preview = input.parentNode.querySelector('[data-image-preview]');
        input.addEventListener('change', function () {
            var file = input.files && input.files[0];
            if (!file || !preview) { if (preview) { preview.hidden = true; } return; }
            if (file.size > 5 * 1024 * 1024) {
                alertField(input, 'Bildet er større enn 5 MB. Velg et mindre bilde.');
                input.value = '';
                preview.hidden = true;
                return;
            }
            alertField(input, '');
            preview.src = URL.createObjectURL(file); // Vises bare i nettleseren, sendes ikke før du lagrer.
            preview.hidden = false;
        });
    }
    function alertField(input, message) {
        var error = input.parentNode.querySelector('.field-validation-valid, .field-validation-error');
        if (error) { error.textContent = message; error.className = message ? 'text-danger field-validation-error' : 'text-danger field-validation-valid'; }
    }

    // ---------- Sidemenyen på mobil ----------
    // På smale skjermer er menyvalgene skjult bak knappen med tre streker. Knappen åpner menyen fra høyre
    // og blir til en X (CSS-animasjon). Menyen lukkes med X-en, Escape, et klikk utenfor eller når man går til en side.
    // Når menyen er lukket på mobil, er den "inert": den kan ikke få fokus med Tab og leses ikke av skjermlesere.
    function initMobileMenu(sidebar) {
        var toggle = sidebar.querySelector('[data-menu-toggle]');
        var panel = sidebar.querySelector('[data-sidebar-panel]');
        var backdrop = sidebar.querySelector('[data-menu-close]');
        if (!toggle || !panel) { return; }
        var mobile = window.matchMedia('(max-width: 900px)');

        function setOpen(open, moveFocus) {
            sidebar.classList.toggle('is-open', open);
            document.body.classList.toggle('menu-open', open);
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
            toggle.setAttribute('aria-label', open ? 'Lukk meny' : 'Åpne meny');
            if (backdrop) { backdrop.hidden = !open; }
            panel.inert = mobile.matches && !open;
            if (moveFocus) {
                var target = open ? panel.querySelector('a, button') : toggle;
                if (target) { target.focus(); }
            }
        }

        toggle.addEventListener('click', function () {
            setOpen(!sidebar.classList.contains('is-open'), true);
        });
        if (backdrop) { backdrop.addEventListener('click', function () { setOpen(false, true); }); }
        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && sidebar.classList.contains('is-open')) { setOpen(false, true); }
        });
        panel.addEventListener('click', function (event) {
            if (event.target.closest('a')) { setOpen(false, false); }
        });
        // Bytter man mellom smal og bred skjerm (f.eks. snur telefonen), lukkes menyen og PC-oppsettet virker som før.
        mobile.addEventListener('change', function () { setOpen(false, false); });
        setOpen(false, false);
    }

    // ---------- Start ----------

    document.querySelectorAll('[data-resource-filter]').forEach(initResourceFilter);
    document.querySelectorAll('[data-sidebar]').forEach(initMobileMenu);
    document.querySelectorAll('[data-point-picker]').forEach(initPointPicker);
    document.querySelectorAll('[data-image-input]').forEach(initImagePreview);
})();
