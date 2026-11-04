// Felles JavaScript for Beredskapskart.
(function () {
    'use strict';

    // Søk + kategorifilter på ressurslister (forsiden og Ressurser).
    // Markup: et element med data-resource-filter som inneholder
    //   input[data-filter-search], knapper med data-filter-chip og data-category,
    //   elementer med data-filter-item, data-category og data-search,
    //   og valgfritt [data-filter-count] og [data-filter-empty].
    // Filtreringen skjer i nettleseren og endrer ingenting på serveren.
    function initResourceFilter(root) {
        var search = root.querySelector('[data-filter-search]');
        var chips = Array.prototype.slice.call(root.querySelectorAll('[data-filter-chip]'));
        var items = Array.prototype.slice.call(root.querySelectorAll('[data-filter-item]'));
        var count = root.querySelector('[data-filter-count]');
        var empty = root.querySelector('[data-filter-empty]');

        function apply() {
            var text = search ? search.value.trim().toLowerCase() : '';
            var active = chips
                .filter(function (chip) { return chip.getAttribute('aria-pressed') === 'true'; })
                .map(function (chip) { return chip.dataset.category; });
            var shown = 0;

            items.forEach(function (item) {
                var categoryOk = active.length === 0 || active.indexOf(item.dataset.category) >= 0;
                var textOk = text === '' || (item.dataset.search || '').indexOf(text) >= 0;
                item.hidden = !(categoryOk && textOk);
                if (!item.hidden) { shown++; }
            });

            if (count) { count.textContent = shown + (shown === 1 ? ' sted' : ' steder'); }
            if (empty) { empty.hidden = shown !== 0; }
        }

        chips.forEach(function (chip) {
            chip.addEventListener('click', function () {
                var on = chip.getAttribute('aria-pressed') === 'true';
                chip.setAttribute('aria-pressed', on ? 'false' : 'true');
                apply();
            });
        });
        if (search) { search.addEventListener('input', apply); }
        apply();
    }

    document.querySelectorAll('[data-resource-filter]').forEach(initResourceFilter);

    // Kartbakgrunn fra OpenStreetMap.
    // ResizeObserver ber Leaflet måle kartet på nytt når boksen endrer størrelse
    // (f.eks. når layouten er ferdig, skriften er lastet eller vinduet blir smalere).
    // Uten dette tegnes kartet bare i en del av boksen.
    window.beredskapTiles = function (map) {
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        }).addTo(map);

        if ('ResizeObserver' in window) {
            new ResizeObserver(function () { map.invalidateSize(); }).observe(map.getContainer());
        }
    };
})();
