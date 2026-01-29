using System;
using System.Collections.Generic;
using System.Text;

namespace Xplore.Contracts;

// Questo è il messaggio che l'API lancerà quando crea un museo
public record MuseumCreatedEvent(Guid Id, string Name);