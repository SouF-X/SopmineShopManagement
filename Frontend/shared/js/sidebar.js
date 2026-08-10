(() => {
  const sidebar = document.querySelector("#sidebar");
  if (!sidebar) return;

  sidebar.innerHTML = `
    <div class="brand">
      <span class="brand-mark">
        <img src="/shared/assets/sopmine-logo.jpeg" alt="Sopmine" width="982" height="1079" />
      </span>
      <span class="brand-copy">
        <strong>Sopmine</strong>
        <small>Sanitaire & Maison</small>
      </span>
    </div>

    <nav class="primary-nav" id="primary-nav">
      <div class="nav-section">
        <span class="nav-label">Pilotage</span>
        <button class="nav-item" data-route="dashboard" data-tooltip="Tableau de bord" aria-label="Tableau de bord" type="button">
          <span class="nav-rail"></span>
          <span class="material-symbols-rounded">space_dashboard</span>
          <span>Tableau de bord</span>
        </button>
      </div>

      <div class="nav-section">
        <span class="nav-label">Catalogue</span>
        <button class="nav-item" data-route="products" data-tooltip="Produits" aria-label="Produits" type="button">
          <span class="nav-rail"></span>
          <span class="material-symbols-rounded">bathroom</span>
          <span>Produits</span>
          <span class="nav-count">0</span>
        </button>
      </div>

      <div class="nav-section">
        <span class="nav-label">Partenaires</span>
        <button class="nav-item" data-route="suppliers" data-tooltip="Fournisseurs" aria-label="Fournisseurs" type="button">
          <span class="nav-rail"></span>
          <span class="material-symbols-rounded">local_shipping</span>
          <span>Fournisseurs</span>
          <span class="nav-count">0</span>
        </button>
        <button class="nav-item" data-route="clients" data-tooltip="Clients" aria-label="Clients" type="button">
          <span class="nav-rail"></span>
          <span class="material-symbols-rounded">person</span>
          <span>Clients</span>
          <span class="nav-count">0</span>
        </button>
      </div>

      <div class="nav-section">
        <span class="nav-label">Commerce</span>
        <div class="nav-family" data-nav-family="purchases">
          <button class="nav-item" data-route="purchases/boncommande" data-nav-root="purchases" data-tooltip="Achats" aria-label="Achats" aria-expanded="false" type="button">
            <span class="nav-rail"></span>
            <span class="material-symbols-rounded">shopping_bag</span>
            <span>Achats</span>
            <span class="nav-count">0</span>
          </button>
          <div class="nav-children">
            <button data-route="purchases/boncommande" type="button">Bons de commande</button>
            <button data-route="purchases/bonreception" type="button">Bons de réception</button>
            <button data-route="purchases/facture" type="button">Factures</button>
            <button data-route="purchases/avoir" type="button">Avoirs</button>
            <button data-route="purchases/lecture-ia" type="button">Lecture IA</button>
          </div>
        </div>
        <div class="nav-family" data-nav-family="sales">
          <button class="nav-item" data-route="sales/devis" data-nav-root="sales" data-tooltip="Ventes" aria-label="Ventes" aria-expanded="false" type="button">
            <span class="nav-rail"></span>
            <span class="material-symbols-rounded">receipt_long</span>
            <span>Ventes</span>
            <span class="nav-count">0</span>
          </button>
          <div class="nav-children">
            <button data-route="sales/devis" type="button">Devis</button>
            <button data-route="sales/bonlivraison" type="button">Bons de livraison</button>
            <button data-route="sales/facture" type="button">Factures</button>
            <button data-route="sales/avoir" type="button">Avoirs</button>
          </div>
        </div>
      </div>

      <div class="nav-section">
        <span class="nav-label">Configuration</span>
        <button class="nav-item" data-route="references" data-tooltip="Référentiels" aria-label="Référentiels" type="button">
          <span class="nav-rail"></span>
          <span class="material-symbols-rounded">category</span>
          <span>Référentiels</span>
        </button>
        <button class="nav-item" data-route="settings/users" data-nav-root="settings" data-tooltip="Paramètres" aria-label="Paramètres" aria-expanded="false" data-admin-only type="button">
          <span class="nav-rail"></span>
          <span class="material-symbols-rounded">settings</span>
          <span>Paramètres</span>
        </button>
      </div>
    </nav>

    <div class="sidebar-foot">
      <button class="workspace-chip profile-menu-trigger" id="sidebar-profile" type="button" aria-label="Ouvrir le menu du profil" aria-haspopup="menu" aria-controls="profile-menu" aria-expanded="false">
        <span class="workspace-icon">SB</span>
        <span><strong>Point de vente</strong><small>Compte Sopmine</small></span>
        <span class="workspace-more material-symbols-rounded" aria-hidden="true">more_horiz</span>
      </button>
      <span class="prototype-note" id="api-status"><i></i><span>Connexion à l’API…</span></span>
    </div>
  `;
  const mobileNavigation = document.createElement("div");
  mobileNavigation.innerHTML = `
    <nav class="mobile-navigation" id="mobile-navigation" aria-label="Navigation mobile">
      <button class="mobile-navigation-item" data-mobile-route="dashboard" data-route="dashboard" type="button">
        <span class="material-symbols-rounded">space_dashboard</span><span>Dashboard</span>
      </button>
      <button class="mobile-navigation-item" data-mobile-route="products" data-route="products" type="button">
        <span class="material-symbols-rounded">bathroom</span><span>Catalogue</span>
      </button>
      <button class="mobile-navigation-item" data-mobile-menu="commerce" type="button" aria-haspopup="dialog" aria-expanded="false">
        <span class="material-symbols-rounded">shopping_bag</span><span>Commerce</span>
      </button>
      <button class="mobile-navigation-item" data-mobile-menu="partners" type="button" aria-haspopup="dialog" aria-expanded="false">
        <span class="material-symbols-rounded">groups</span><span>Partenaires</span>
      </button>
      <button class="mobile-navigation-item" data-mobile-menu="more" type="button" aria-haspopup="dialog" aria-expanded="false">
        <span class="material-symbols-rounded">more_horiz</span><span>Plus</span>
      </button>
    </nav>

    <section class="mobile-section-sheet" id="mobile-section-sheet" hidden aria-hidden="true">
      <div class="mobile-section-sheet-panel" role="dialog" aria-modal="true" aria-labelledby="mobile-section-sheet-title">
        <header class="mobile-section-sheet-head">
          <button class="icon-button mobile-sheet-back" type="button" data-mobile-sheet-back hidden aria-label="Retour aux choix"><span class="material-symbols-rounded">arrow_back</span></button>
          <div><span class="eyebrow">Navigation</span><h2 id="mobile-section-sheet-title" data-mobile-sheet-title>Commerce</h2></div>
          <button class="icon-button" type="button" data-mobile-sheet-close aria-label="Fermer la navigation"><span class="material-symbols-rounded">close</span></button>
        </header>
        <div class="mobile-section-sheet-content">
          <div class="mobile-commerce-choice-list" data-mobile-commerce-choice-list hidden>
            <p class="mobile-sheet-intro">Choisissez le flux à ouvrir</p>
            <div class="mobile-commerce-choice-grid">
              <button class="mobile-commerce-choice" data-mobile-commerce-choice="purchases" data-mobile-purchases type="button">
                <span class="mobile-commerce-choice-icon material-symbols-rounded">shopping_bag</span>
                <span><strong>Achats</strong><small>Fournisseurs, stock et factures</small></span>
                <span class="material-symbols-rounded mobile-commerce-choice-arrow">chevron_right</span>
              </button>
              <button class="mobile-commerce-choice" data-mobile-commerce-choice="sales" type="button">
                <span class="mobile-commerce-choice-icon material-symbols-rounded">receipt_long</span>
                <span><strong>Ventes</strong><small>Clients, devis et factures</small></span>
                <span class="material-symbols-rounded mobile-commerce-choice-arrow">chevron_right</span>
              </button>
            </div>
          </div>

          <div class="mobile-sheet-group" data-mobile-sheet-group="purchases" hidden>
            <div class="mobile-sheet-options" data-mobile-purchases>
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="purchases/boncommande" type="button"><span class="material-symbols-rounded">request_quote</span><span><strong>Bons de commande</strong><small>Pr&eacute;parer un achat</small></span></button>
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="purchases/bonreception" type="button"><span class="material-symbols-rounded">inventory</span><span><strong>Bons de r&eacute;ception</strong><small>Entr&eacute;es en stock</small></span></button>
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="purchases/facture" type="button"><span class="material-symbols-rounded">receipt_long</span><span><strong>Factures</strong><small>Suivre les achats</small></span></button>
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="purchases/avoir" type="button"><span class="material-symbols-rounded">assignment_return</span><span><strong>Avoirs</strong><small>Retours fournisseur</small></span></button>
              <button class="mobile-sheet-item mobile-sheet-item--accent" data-mobile-sheet-item data-route="purchases/lecture-ia" type="button"><span class="material-symbols-rounded">document_scanner</span><span><strong>Lecture IA</strong><small>Scanner un document</small></span></button>
            </div>
          </div>

          <div class="mobile-sheet-group" data-mobile-sheet-group="sales" hidden>
            <div class="mobile-sheet-options">
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="sales/devis" type="button"><span class="material-symbols-rounded">request_quote</span><span><strong>Devis</strong><small>Pr&eacute;parer une vente</small></span></button>
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="sales/bonlivraison" type="button"><span class="material-symbols-rounded">local_shipping</span><span><strong>Bons de livraison</strong><small>Sorties client</small></span></button>
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="sales/facture" type="button"><span class="material-symbols-rounded">receipt_long</span><span><strong>Factures</strong><small>Ventes factur&eacute;es</small></span></button>
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="sales/avoir" type="button"><span class="material-symbols-rounded">assignment_return</span><span><strong>Avoirs</strong><small>Retours client</small></span></button>
            </div>
          </div>

          <div class="mobile-sheet-group" data-mobile-sheet-group="partners" hidden>
            <div class="mobile-sheet-options">
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="suppliers" type="button"><span class="material-symbols-rounded">local_shipping</span><span><strong>Fournisseurs</strong><small>G&eacute;rer les partenaires d'achat</small></span></button>
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="clients" type="button"><span class="material-symbols-rounded">person</span><span><strong>Clients</strong><small>G&eacute;rer les partenaires de vente</small></span></button>
            </div>
          </div>

          <div class="mobile-sheet-group" data-mobile-sheet-group="more" hidden>
            <div class="mobile-sheet-options">
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="references" type="button"><span class="material-symbols-rounded">category</span><span><strong>R&eacute;f&eacute;rentiels</strong><small>Familles, unit&eacute;s et r&egrave;gles</small></span></button>
              <button class="mobile-sheet-item" data-mobile-sheet-item data-route="settings/users" data-admin-only type="button"><span class="material-symbols-rounded">settings</span><span><strong>Param&egrave;tres</strong><small>Configuration de l'espace</small></span></button>
            </div>
          </div>
        </div>
      </div>
    </section>
  `;
  document.body.appendChild(mobileNavigation);
})();
