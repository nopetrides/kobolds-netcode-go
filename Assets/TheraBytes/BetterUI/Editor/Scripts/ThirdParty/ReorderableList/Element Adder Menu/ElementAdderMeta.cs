// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System.Collections.Generic;
using System.Linq;

namespace TheraBytes.BetterUi.Editor.ThirdParty
{

	/// <summary>
	/// Provides meta information which is useful when creating new implementations of
	/// the <see cref="IElementAdderMenuBuilder{TContext}"/> interface.
	/// </summary>
	public static class ElementAdderMeta {

		#region Adder Menu Command Types

		private static Dictionary<System.Type, Dictionary<System.Type, List<System.Type>>> s_ContextMap = new Dictionary<System.Type, Dictionary<System.Type, List<System.Type>>>();

		private static IEnumerable<System.Type> GetMenuCommandTypes<TContext>() {
			return
				from a in System.AppDomain.CurrentDomain.GetAssemblies()
				from t in a.GetTypes()
				where t.IsClass && !t.IsAbstract && t.IsDefined(typeof(ElementAdderMenuCommandAttribute), false)
				where typeof(IElementAdderMenuCommand<TContext>).IsAssignableFrom(t)
				select t;
		}

		/// <summary>
		/// Gets an array of the <see cref="IElementAdderMenuCommand{TContext}"/> types
		/// that are associated with the specified <paramref name="contractType"/>.
		/// </summary>
		/// <typeparam name="TContext">Type of the context object that elements can be added to.</typeparam>
		/// <param name="contractType">Contract type of addable elements.</param>
		/// <returns>
		/// An array containing zero or more <see cref="System.System.Type"/>.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">
		/// If <paramref name="contractType"/> is <c>null</c>.
		/// </exception>
		/// <seealso cref="GetMenuCommands{TContext}(System.Type)"/>
		public static System.Type[] GetMenuCommandTypes<TContext>(System.Type contractType) {
			if (contractType == null)
				throw new System.ArgumentNullException("contractType");

			Dictionary<System.Type, List<System.Type>> contractMap;
			List<System.Type> commandTypes;
			if (s_ContextMap.TryGetValue(typeof(TContext), out contractMap)) {
				if (contractMap.TryGetValue(contractType, out commandTypes))
					return commandTypes.ToArray();
			}
			else {
				contractMap = new Dictionary<System.Type, List<System.Type>>();
				s_ContextMap[typeof(TContext)] = contractMap;
			}

			commandTypes = new List<System.Type>();

			foreach (var commandType in GetMenuCommandTypes<TContext>()) {
				var attributes = (ElementAdderMenuCommandAttribute[])System.Attribute.GetCustomAttributes(commandType, typeof(ElementAdderMenuCommandAttribute));
				if (!attributes.Any(a => a.ContractType == contractType))
					continue;

				commandTypes.Add(commandType);
			}

			contractMap[contractType] = commandTypes;
			return commandTypes.ToArray();
		}

		/// <summary>
		/// Gets an array of <see cref="IElementAdderMenuCommand{TContext}"/> instances
		/// that are associated with the specified <paramref name="contractType"/>.
		/// </summary>
		/// <typeparam name="TContext">Type of the context object that elements can be added to.</typeparam>
		/// <param name="contractType">Contract type of addable elements.</param>
		/// <returns>
		/// An array containing zero or more <see cref="IElementAdderMenuCommand{TContext}"/> instances.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">
		/// If <paramref name="contractType"/> is <c>null</c>.
		/// </exception>
		/// <seealso cref="GetMenuCommandTypes{TContext}(System.Type)"/>
		public static IElementAdderMenuCommand<TContext>[] GetMenuCommands<TContext>(System.Type contractType) {
			var commandTypes = GetMenuCommandTypes<TContext>(contractType);
			var commands = new IElementAdderMenuCommand<TContext>[commandTypes.Length];
			for (int i = 0; i < commandTypes.Length; ++i)
				commands[i] = (IElementAdderMenuCommand<TContext>)System.Activator.CreateInstance(commandTypes[i]);
			return commands;
		}

		#endregion

		#region Concrete Element Types

		private static Dictionary<System.Type, System.Type[]> s_ConcreteElementTypes = new Dictionary<System.Type, System.Type[]>();

		private static IEnumerable<System.Type> GetConcreteElementTypesHelper(System.Type contractType) {
			if (contractType == null)
				throw new System.ArgumentNullException("contractType");

			System.Type[] concreteTypes;
			if (!s_ConcreteElementTypes.TryGetValue(contractType, out concreteTypes)) {
				concreteTypes =
					(from a in System.AppDomain.CurrentDomain.GetAssemblies()
					 from t in a.GetTypes()
					 where t.IsClass && !t.IsAbstract && contractType.IsAssignableFrom(t)
					 orderby t.Name
					 select t
					).ToArray();
				s_ConcreteElementTypes[contractType] = concreteTypes;
			}

			return concreteTypes;
		}

		/// <summary>
		/// Gets a filtered array of the concrete element types that implement the
		/// specified <paramref name="contractType"/>.
		/// </summary>
		/// <remarks>
		/// <para>A type is excluded from the resulting array when one or more of the
		/// specified <paramref name="filters"/> returns a value of <c>false</c>.</para>
		/// </remarks>
		/// <param name="contractType">Contract type of addable elements.</param>
		/// <param name="filters">An array of zero or more filters.</param>
		/// <returns>
		/// An array of zero or more concrete element types.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">
		/// If <paramref name="contractType"/> is <c>null</c>.
		/// </exception>
		/// <seealso cref="GetConcreteElementTypes(System.Type)"/>
		public static System.Type[] GetConcreteElementTypes(System.Type contractType, System.Func<System.Type, bool>[] filters) {
			return
				(from t in GetConcreteElementTypesHelper(contractType)
				 where IsTypeIncluded(t, filters)
				 select t
				).ToArray();
		}

        /// <summary>
        /// Gets an array of all the concrete element types that implement the specified
        /// <paramref name="contractType"/>.
        /// </summary>
        /// <param name="contractType">Contract type of addable elements.</param>
        /// <returns>
        /// An array of zero or more concrete element types.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="contractType"/> is <c>null</c>.
        /// </exception>
        /// <seealso cref="GetConcreteElementTypes(System.Type, System.Func{System.Type, bool}[])"/>
        public static System.Type[] GetConcreteElementTypes(System.Type contractType) {
			return GetConcreteElementTypesHelper(contractType).ToArray();
		}

		private static bool IsTypeIncluded(System.Type concreteType, System.Func<System.Type, bool>[] filters) {
			if (filters != null)
				foreach (var filter in filters)
					if (!filter(concreteType))
						return false;
			return true;
		}

		#endregion

	}

}
